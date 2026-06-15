using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Extensions;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Auth;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Email;

namespace OptionsEdge.API.Features.Auth;

public static class AuthEndpoints
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");

        // POST /api/v1/auth/register
        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<ApplicationUser> userManager,
            IOptions<AuthSettings> authSettings,
            IEmailService emailService,
            HttpContext ctx,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.DisplayName))
                return Results.BadRequest(new { error = "Email, password and display name are required." });

            var user = new ApplicationUser
            {
                UserName         = req.Email,
                Email            = req.Email,
                DisplayName      = req.DisplayName,
                SubscriptionPlan = "free",
                WalletBalance    = 0m,
                AiCallsResetAt   = DateTimeOffset.UtcNow,
                IsActive         = true,
                CreatedAt        = DateTimeOffset.UtcNow,
                UpdatedAt        = DateTimeOffset.UtcNow,
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) });

            var settings = authSettings.Value;
            if (!settings.RequireEmailConfirmation)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);
            }
            else
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var link  = BuildConfirmationLink(config, user.Id, token);
                await emailService.SendEmailConfirmationAsync(user.Email!, user.DisplayName, link, ct);
            }

            return Results.Ok(new { message = settings.RequireEmailConfirmation
                ? "Registration successful. Please check your email to confirm your account."
                : "Registration successful. You can now log in." });
        }).WithName("Register");

        // POST /api/v1/auth/confirm-email
        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest req,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByIdAsync(req.UserId);
            if (user is null)
                return Results.BadRequest(new { error = "Invalid confirmation request." });

            var result = await userManager.ConfirmEmailAsync(user, req.Token);
            if (!result.Succeeded)
                return Results.BadRequest(new { error = "Invalid or expired confirmation token." });

            return Results.Ok(new { message = "Email confirmed. You can now log in." });
        }).WithName("ConfirmEmail");

        // POST /api/v1/auth/resend-confirmation — always 200 to prevent user enumeration
        group.MapPost("/resend-confirmation", async (
            ResendConfirmationRequest req,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is not null && !user.EmailConfirmed)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var link  = BuildConfirmationLink(config, user.Id, token);
                await emailService.SendEmailConfirmationAsync(user.Email!, user.DisplayName, link, ct);
            }

            return Results.Ok(new { message = "If an account exists for that email, a confirmation link has been sent." });
        }).WithName("ResendConfirmation");

        // POST /api/v1/auth/login
        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<AuthSettings> authSettings,
            JwtService jwt,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.BadRequest(new { error = InvalidCredentialsMessage });

            var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: authSettings.Value.EnableLockout);

            if (passwordCheck.IsLockedOut)
                return Results.Json(new { error = "Account locked due to too many failed attempts. Try again later." }, statusCode: StatusCodes.Status423Locked);

            if (!passwordCheck.Succeeded)
                return Results.BadRequest(new { error = InvalidCredentialsMessage });

            if (!user.EmailConfirmed)
                return Results.BadRequest(new { error = "Please confirm your email before logging in." });

            if (await userManager.GetTwoFactorEnabledAsync(user))
                return Results.Ok(new TwoFactorRequiredResponse(true, user.Email!));

            return Results.Ok(await IssueAuthResponseAsync(user, userManager, jwt, db, ct));
        }).WithName("Login");

        // POST /api/v1/auth/two-factor — completes login when 2FA is enabled
        group.MapPost("/two-factor", async (
            TwoFactorRequest req,
            UserManager<ApplicationUser> userManager,
            JwtService jwt,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.BadRequest(new { error = InvalidCredentialsMessage });

            var validToken = await userManager.VerifyTwoFactorTokenAsync(
                user, TokenOptions.DefaultAuthenticatorProvider, req.Code);

            if (!validToken)
            {
                var recoveryResult = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, req.Code);
                validToken = recoveryResult.Succeeded;
            }

            if (!validToken)
                return Results.BadRequest(new { error = "Invalid verification code." });

            return Results.Ok(await IssueAuthResponseAsync(user, userManager, jwt, db, ct));
        }).WithName("VerifyTwoFactor");

        // POST /api/v1/auth/refresh — rotates the refresh token
        group.MapPost("/refresh", async (
            RefreshRequest req,
            UserManager<ApplicationUser> userManager,
            JwtService jwt,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken, ct);
            if (existing is null || existing.IsRevoked || existing.ExpiresAt < DateTimeOffset.UtcNow)
                return Results.BadRequest(new { error = "Invalid or expired refresh token." });

            var user = await userManager.FindByIdAsync(existing.UserId.ToString());
            if (user is null)
                return Results.BadRequest(new { error = "Invalid or expired refresh token." });

            existing.IsRevoked = true;
            db.RefreshTokens.Update(existing);

            var response = await IssueAuthResponseAsync(user, userManager, jwt, db, ct);

            return Results.Ok(response);
        }).WithName("RefreshToken");

        // POST /api/v1/auth/logout — revokes the supplied refresh token
        group.MapPost("/logout", async (
            LogoutRequest req,
            AppDbContext db,
            HttpContext ctx,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(
                t => t.Token == req.RefreshToken && t.UserId == userId,
                ct);
            if (existing is not null)
            {
                existing.IsRevoked = true;
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { message = "Logged out." });
        }).WithName("Logout")
          .RequireAuthorization();

        // POST /api/v1/auth/forgot-password — always 200 to prevent user enumeration
        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest req,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            HttpContext ctx,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is not null && user.EmailConfirmed)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var link  = BuildResetLink(config, user.Email!, token);
                await emailService.SendPasswordResetAsync(user.Email!, user.DisplayName, link, ct);
            }

            return Results.Ok(new { message = "If an account exists for that email, a password reset link has been sent." });
        }).WithName("ForgotPassword");

        // POST /api/v1/auth/reset-password
        group.MapPost("/reset-password", async (
            ResetPasswordRequest req,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            if (req.NewPassword != req.ConfirmPassword)
                return Results.BadRequest(new { error = "Passwords do not match." });

            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.BadRequest(new { error = "Invalid or expired reset token." });

            var result = await userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
            if (!result.Succeeded)
                return Results.BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) });

            return Results.Ok(new { message = "Password reset. You can now log in with your new password." });
        }).WithName("ResetPassword");

        // POST /api/v1/auth/enable-2fa — generates an authenticator key for the current user
        group.MapPost("/enable-2fa", async (
            HttpContext ctx,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var user   = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Results.NotFound();

            var key = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                key = await userManager.GetAuthenticatorKeyAsync(user);
            }

            var authenticatorUri = BuildAuthenticatorUri(user.Email!, key!);
            return Results.Ok(new EnableTwoFactorResponse(key!, authenticatorUri));
        }).WithName("EnableTwoFactor")
          .RequireAuthorization();

        // POST /api/v1/auth/verify-2fa-setup — confirms the TOTP code and turns 2FA on
        group.MapPost("/verify-2fa-setup", async (
            VerifyTwoFactorSetupRequest req,
            HttpContext ctx,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var user   = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Results.NotFound();

            var isValid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, req.Code);
            if (!isValid)
                return Results.BadRequest(new { error = "Invalid verification code." });

            await userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return Results.Ok(new VerifyTwoFactorSetupResponse(true, recoveryCodes?.ToArray() ?? []));
        }).WithName("VerifyTwoFactorSetup")
          .RequireAuthorization();

        // POST /api/v1/auth/disable-2fa
        group.MapPost("/disable-2fa", async (
            DisableTwoFactorRequest req,
            HttpContext ctx,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var user   = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Results.NotFound();

            if (!await userManager.CheckPasswordAsync(user, req.Password))
                return Results.BadRequest(new { error = "Incorrect password." });

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);

            return Results.Ok(new { message = "Two-factor authentication disabled." });
        }).WithName("DisableTwoFactor")
          .RequireAuthorization();

        // GET /api/v1/auth/me
        group.MapGet("/me", async (
            HttpContext ctx,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId(config);
            var user   = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Results.NotFound();

            return Results.Ok(new MeResponse(
                user.Id,
                user.Email!,
                user.DisplayName,
                user.SubscriptionPlan,
                user.WalletBalance,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.CreatedAt.ToString("O")));
        }).WithName("Me")
          .RequireAuthorization();

        // POST /api/v1/auth/change-password
        group.MapPost("/change-password", async (
            ChangePasswordRequest req,
            HttpContext ctx,
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            if (req.NewPassword != req.ConfirmPassword)
                return Results.BadRequest(new { error = "Passwords do not match." });

            var userId = ctx.GetUserId(config);
            var user   = await userManager.FindByIdAsync(userId.ToString());
            if (user is null) return Results.NotFound();

            var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            if (!result.Succeeded)
                return Results.BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) });

            return Results.Ok(new { message = "Password changed." });
        }).WithName("ChangePassword")
          .RequireAuthorization();
    }

    private static async Task<AuthResponse> IssueAuthResponseAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        JwtService jwt,
        AppDbContext db,
        CancellationToken ct)
    {
        var (accessToken, expiresAt) = jwt.GenerateAccessToken(user);
        var refreshToken = jwt.GenerateRefreshToken();
        var refreshDays  = jwt.RefreshTokenDays;

        db.RefreshTokens.Add(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshDays),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresAt.ToString("O"),
            user.Id,
            user.DisplayName,
            user.Email!,
            user.SubscriptionPlan,
            await userManager.GetTwoFactorEnabledAsync(user));
    }

    private static string BuildConfirmationLink(
        IConfiguration config, Guid userId, string token)
    {
        var frontend = config["App:FrontendUrl"] 
            ?? "http://localhost:5173";
        var encoded = Uri.EscapeDataString(token);
        return $"{frontend}/verify-email" +
            $"?userId={userId}&token={encoded}";
    }

    private static string BuildResetLink(
        IConfiguration config, string email, string token)
    {
        var frontend = config["App:FrontendUrl"] 
            ?? "http://localhost:5173";
        var encoded = Uri.EscapeDataString(token);
        return $"{frontend}/reset-password" +
            $"?email={Uri.EscapeDataString(email)}" +
            $"&token={encoded}";
    }

    private static string BuildAuthenticatorUri(string email, string unformattedKey)
    {
        const string issuer = "OptionsEdge";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
               $"?secret={unformattedKey}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }
}

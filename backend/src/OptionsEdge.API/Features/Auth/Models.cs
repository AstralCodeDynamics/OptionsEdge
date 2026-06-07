namespace OptionsEdge.API.Features.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record ConfirmEmailRequest(string UserId, string Token);
public record ResendConfirmationRequest(string Email);
public record LoginRequest(string Email, string Password);
public record TwoFactorRequest(string Email, string Code, bool RememberMachine = false);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record VerifyTwoFactorSetupRequest(string Code);
public record DisableTwoFactorRequest(string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string AccessTokenExpiry,
    string DisplayName,
    string Email,
    string SubscriptionPlan,
    bool TwoFactorEnabled);

public record TwoFactorRequiredResponse(bool TwoFactorRequired, string Email);

public record EnableTwoFactorResponse(string SharedKey, string AuthenticatorUri);

public record VerifyTwoFactorSetupResponse(bool Enabled, string[] RecoveryCodes);

public record MeResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string SubscriptionPlan,
    decimal WalletBalance,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    string CreatedAt);

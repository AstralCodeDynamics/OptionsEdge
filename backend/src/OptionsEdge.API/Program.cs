using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.Auth;
using OptionsEdge.API.Features.Backtest;
using OptionsEdge.API.Features.Billing;
using OptionsEdge.API.Features.Chat;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Features.Indicators;
using OptionsEdge.API.Features.Market;
using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Features.Positions;
using OptionsEdge.API.Features.Signals;
using OptionsEdge.API.Features.Usage;
using OptionsEdge.API.Infrastructure.Auth;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Email;
using OptionsEdge.API.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();

// AuthSettings drives dev-friendly toggles for email confirmation, 2FA, lockout and email sending
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings"));
var authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>() ?? new AuthSettings();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequireLowercase        = true;
        options.Password.RequireUppercase        = true;
        options.Password.RequireNonAlphanumeric  = true;
        options.Password.RequiredLength          = 8;

        options.Lockout.DefaultLockoutTimeSpan   = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts  = 5;
        options.Lockout.AllowedForNewUsers       = authSettings.EnableLockout;

        options.SignIn.RequireConfirmedEmail     = authSettings.RequireEmailConfirmation;

        options.User.RequireUniqueEmail          = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret  = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.FromSeconds(30),
        };

        // SignalR sends the access token via query string (?access_token=...) since browsers
        // can't set Authorization headers on WebSocket connections.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IEmailService>(sp =>
    authSettings.SendRealEmails
        ? sp.GetRequiredService<EmailService>()
        : sp.GetRequiredService<DevEmailService>());
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<DevEmailService>();

builder.Services.AddMemoryCache();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// Feature services
builder.Services.AddMarketServices();
builder.Services.AddIndicatorServices();
builder.Services.AddOptionsServices();
builder.Services.AddSignalServices();
builder.Services.AddPositionServices();
builder.Services.AddChatServices();
builder.Services.AddGrowwServices();
builder.Services.AddBacktestServices();
builder.Services.AddUsageServices();

// Background workers
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddHostedService<PositionMonitorWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.SeedAsync(app.Services, app.Logger);
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithName("Health");

// Feature endpoints
app.MapAuthEndpoints();
app.MapMarketEndpoints();
app.MapIndicatorEndpoints();
app.MapOptionsEndpoints();
app.MapSignalEndpoints();
app.MapPositionEndpoints();
app.MapChatEndpoints();
app.MapGrowwEndpoints();
app.MapBacktestEndpoints();
app.MapUsageEndpoints();
app.MapBillingEndpoints();

// SignalR hubs
app.MapHub<MarketHub>("/hubs/market");

app.Run();

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OptionsEdge.API.Common.Configuration;
using OptionsEdge.API.Common.Options;
using OptionsEdge.API.Common.Time;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.AI;
using OptionsEdge.API.Features.Auth;
using OptionsEdge.API.Features.Backtest;
using OptionsEdge.API.Features.Billing;
using OptionsEdge.API.Features.Chat;
using OptionsEdge.API.Features.Config;
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
using OptionsEdge.API.Infrastructure.Logging;
using OptionsEdge.API.Infrastructure.Middleware;
using OptionsEdge.API.Infrastructure.Security;
using OptionsEdge.API.Infrastructure.SignalR;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With<IstTimestampEnricher>()
    .WriteTo.Console(outputTemplate:
        "[{IstTimestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var logFileOptions = context.Configuration.GetSection(LogFileOptions.SectionName).Get<LogFileOptions>() ?? new LogFileOptions();
    var logDirectory = LogFilePathResolver.Resolve(context.HostingEnvironment.ContentRootPath, logFileOptions.Directory);
    Directory.CreateDirectory(logDirectory);

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<IstTimestampEnricher>()
        .Enrich.WithProperty("Application", "OptionsEdge.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(outputTemplate:
            "[{IstTimestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: Path.Combine(logDirectory, $"{logFileOptions.FileNamePrefix}-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: null,
            shared: true,
            outputTemplate: "[{IstTimestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            flushToDiskInterval: TimeSpan.FromSeconds(1));
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<LogFileOptions>(builder.Configuration.GetSection(LogFileOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();
builder.Services.Configure<LotSizeOptions>(builder.Configuration.GetSection("LotSizes"));

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
            ClockSkew                = TimeSpan.FromMinutes(5),
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
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
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
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection("AI"));
builder.Services.AddAIServices();

// Background workers
builder.Services.AddHostedService<MarketDataWorker>();
builder.Services.AddHostedService<PositionMonitorWorker>();
builder.Services.AddHostedService<AutoSignalWorker>();
builder.Services.AddHostedService<WeeklyConsistencyCheckWorker>();
builder.Services.AddSingleton<LogFileMaintenanceService>();
builder.Services.AddHostedService<LogFileCleanupWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.SeedAsync(app.Services, app.Logger);
}

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
    {
        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
            return LogEventLevel.Warning;

        return LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
    };
});

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();
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
app.MapConfigEndpoints();
app.MapGrowwEndpoints();
app.MapBacktestEndpoints();
app.MapUsageEndpoints();
app.MapBillingEndpoints();
app.MapAICredentialEndpoints();

// SignalR hubs
app.MapHub<MarketHub>("/hubs/market")
   .RequireAuthorization();

await app.RunAsync();
}
catch (HostAbortedException)
{
    // Expected during EF Core design-time operations such as migrations bundle creation.
}
catch (Exception ex)
{
    Log.Fatal(ex, "OptionsEdge API terminated unexpectedly during startup");
}
finally
{
    await Log.CloseAndFlushAsync();
}

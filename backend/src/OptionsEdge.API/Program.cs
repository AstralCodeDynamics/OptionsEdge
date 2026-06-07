using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Features.Backtest;
using OptionsEdge.API.Features.Chat;
using OptionsEdge.API.Features.Groww;
using OptionsEdge.API.Features.Indicators;
using OptionsEdge.API.Features.Market;
using OptionsEdge.API.Features.Options;
using OptionsEdge.API.Features.Positions;
using OptionsEdge.API.Features.Signals;
using OptionsEdge.API.Infrastructure.Background;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();

builder.Services.AddAuthentication().AddJwtBearer();

builder.Services.AddAuthorization();

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
app.MapMarketEndpoints();
app.MapIndicatorEndpoints();
app.MapOptionsEndpoints();
app.MapSignalEndpoints();
app.MapPositionEndpoints();
app.MapChatEndpoints();
app.MapGrowwEndpoints();
app.MapBacktestEndpoints();

// SignalR hubs
app.MapHub<MarketHub>("/hubs/market");

app.Run();

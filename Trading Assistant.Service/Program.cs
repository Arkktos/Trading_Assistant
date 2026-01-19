using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Trading_Assistant.Service.Configuration;
using Trading_Assistant.Service.Services;
using Trading_Assistant.Service.State;

var builder = Host.CreateApplicationBuilder(args);

// Configure Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "TradingAssistantService";
});

// Load configuration
var appConfig = new AppConfig();
builder.Configuration.Bind(appConfig);

// Override with custom environment variables
if (Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY") is { } alphaKey)
    appConfig.AlphaVantage.ApiKey = alphaKey;

if (Environment.GetEnvironmentVariable("GMAIL_APP_PASSWORD") is { } gmailPassword)
    appConfig.Email.Password = gmailPassword;

// Register configuration sections
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(appConfig.Trading);
builder.Services.AddSingleton(appConfig.Claude);
builder.Services.AddSingleton(appConfig.AlphaVantage);
builder.Services.AddSingleton(appConfig.Email);
builder.Services.AddSingleton(appConfig.Schedule);

// Register Configuration Service
var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
builder.Services.AddSingleton<IConfigurationService>(sp =>
    new ConfigurationService(configPath, sp.GetRequiredService<ILogger<ConfigurationService>>()));

// Register Core services
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<IClaudeAnalysisService, ClaudeAnalysisService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// Register Service state
builder.Services.AddSingleton<ServiceState>();

// Register Background Service
builder.Services.AddSingleton<Trading_Assistant.Service.AnalysisBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Trading_Assistant.Service.AnalysisBackgroundService>());

// Register IPC Server Host
builder.Services.AddHostedService<Trading_Assistant.Service.IpcServerHost>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog(new EventLogSettings
    {
        SourceName = "TradingAssistantService",
        LogName = "Application"
    });
}

builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Trading Assistant Service starting...");
logger.LogInformation("Configuration path: {Path}", configPath);

// Run startup validation (skip with --skip-validation flag)
var skipValidation = args.Contains("--skip-validation");

if (skipValidation)
{
    logger.LogWarning("Startup validation skipped (--skip-validation flag)");
}
else
{
    var validator = new StartupValidator(
        host.Services.GetRequiredService<ILogger<StartupValidator>>(),
        appConfig);
    var validationResult = await validator.ValidateAsync();

    if (validationResult.HasCriticalErrors)
    {
        logger.LogError("Service cannot start due to critical configuration errors");
        logger.LogError("Please fix the following issues:");
        foreach (var error in validationResult.Errors)
        {
            logger.LogError("  - {Error}", error);
        }
        logger.LogInformation("Tip: Use --skip-validation to bypass validation during development");
        Environment.Exit(1);
        return;
    }
}

logger.LogInformation("Watched assets: {Count}", appConfig.Trading.WatchedAssets.Count);

await host.RunAsync();

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Trading_Assistant.Service.Configuration;
using Trading_Assistant.Service.Services;
using Trading_Assistant.Service.State;

var logFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "Trading Assistant", "logs");
Directory.CreateDirectory(logFolder);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .WriteTo.File(
        path: Path.Combine(logFolder, "service-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

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

// Register configuration sections
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(appConfig.Trading);
builder.Services.AddSingleton(appConfig.Claude);
builder.Services.AddSingleton(appConfig.AlphaVantage);
builder.Services.AddSingleton(appConfig.Schedule);

// Register Configuration Service
var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
builder.Services.AddSingleton<IConfigurationService>(sp =>
    new ConfigurationService(configPath, sp.GetRequiredService<ILogger<ConfigurationService>>()));

// Register Core services
builder.Services.AddSingleton<IPortfolioService, PortfolioService>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<IClaudeAnalysisService, ClaudeAnalysisService>();

// Register Service state
builder.Services.AddSingleton<ServiceState>();

// Register Background Service
builder.Services.AddSingleton<Trading_Assistant.Service.AnalysisBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Trading_Assistant.Service.AnalysisBackgroundService>());

// Register IPC Server Host
builder.Services.AddHostedService<Trading_Assistant.Service.IpcServerHost>();

// Configure logging — Serilog with rolling file
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

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

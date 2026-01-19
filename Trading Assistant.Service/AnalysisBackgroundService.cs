using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.Configuration;
using Trading_Assistant.Service.Services;
using Trading_Assistant.Service.State;

namespace Trading_Assistant.Service;

public class AnalysisBackgroundService : BackgroundService
{
    private readonly ILogger<AnalysisBackgroundService> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly IClaudeAnalysisService _claudeService;
    private readonly IEmailService _emailService;
    private readonly IConfigurationService _configService;
    private readonly ServiceState _state;
    private readonly ScheduleConfig _scheduleConfig;
    private Timer? _analysisTimer;
    private bool _analysisRequested = false;
    private readonly object _analysisLock = new();

    public AnalysisBackgroundService(
        ILogger<AnalysisBackgroundService> logger,
        IMarketDataService marketDataService,
        IClaudeAnalysisService claudeService,
        IEmailService emailService,
        IConfigurationService configService,
        ServiceState state,
        ScheduleConfig scheduleConfig)
    {
        _logger = logger;
        _marketDataService = marketDataService;
        _claudeService = claudeService;
        _emailService = emailService;
        _configService = configService;
        _state = state;
        _scheduleConfig = scheduleConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analysis Background Service starting");
        UpdateNextScheduledAnalysis();

        _analysisTimer = new Timer(
            async _ => await CheckAndRunAnalysisAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Analysis Background Service stopping");
        }
    }

    private async Task CheckAndRunAnalysisAsync()
    {
        bool shouldRunManual;
        lock (_analysisLock)
        {
            shouldRunManual = _analysisRequested;
            _analysisRequested = false;
        }

        if (shouldRunManual)
        {
            _logger.LogInformation("Manual analysis requested");
            await PerformAnalysisAsync();
            return;
        }

        if (_state.NextScheduledAnalysis.HasValue && DateTime.Now >= _state.NextScheduledAnalysis.Value)
        {
            _logger.LogInformation("Scheduled analysis time reached");
            await PerformAnalysisAsync();
            UpdateNextScheduledAnalysis();
        }
    }

    public void RequestManualAnalysis()
    {
        lock (_analysisLock)
        {
            _analysisRequested = true;
        }
        _logger.LogInformation("Manual analysis has been requested");
    }

    public async Task PerformAnalysisAsync()
    {
        if (_state.IsAnalyzing)
        {
            _logger.LogWarning("Analysis already in progress, skipping");
            return;
        }

        try
        {
            _logger.LogInformation("Starting market analysis");
            await _state.SetAnalyzingAsync(true);

            var config = _configService.LoadConfiguration();
            var watchedAssets = config.Trading.WatchedAssets;

            if (!watchedAssets.Any())
            {
                _logger.LogWarning("No watched assets configured");
                await _state.SetAnalyzingAsync(false);
                return;
            }

            _logger.LogInformation("Fetching market data for {Count} assets", watchedAssets.Count);
            var marketDataList = await _marketDataService.GetMarketDataForAssetsAsync(watchedAssets);

            if (!marketDataList.Any())
            {
                _logger.LogWarning("No market data retrieved");
                await _state.SetAnalyzingAsync(false);
                return;
            }

            _logger.LogInformation("Retrieved market data for {Count} assets", marketDataList.Count);
            _logger.LogInformation("Analyzing with Claude AI...");

            var analysisResult = await _claudeService.AnalyzeMarketDataAsync(
                marketDataList,
                config.Trading.AvailableCapital,
                config.Trading.RiskProfile);

            await _state.SetLastAnalysisAsync(analysisResult);
            _logger.LogInformation("Analysis completed. Found {Count} opportunities", analysisResult.Opportunities.Count);

            if (_state.ShouldSendEmail())
            {
                _logger.LogInformation("No UI registered, sending email notification");
                await _emailService.SendAnalysisReportAsync(analysisResult, marketDataList);
                _logger.LogInformation("Email sent successfully");
            }
            else
            {
                _logger.LogInformation("UI is active ({Count} clients registered), skipping email", _state.RegisteredUICount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during market analysis");
            await _state.SetAnalyzingAsync(false);
        }
    }

    private void UpdateNextScheduledAnalysis()
    {
        if (!TimeOnly.TryParse(_scheduleConfig.AnalysisTime, out var analysisTime))
        {
            analysisTime = new TimeOnly(8, 0);
        }

        var timeZone = GetConfiguredTimeZone();
        var nowUtc = DateTime.UtcNow;
        var nowInTimeZone = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);

        // Calculate next run in the configured timezone
        var nextRunInTimeZone = nowInTimeZone.Date.Add(analysisTime.ToTimeSpan());

        if (nextRunInTimeZone <= nowInTimeZone)
        {
            nextRunInTimeZone = nextRunInTimeZone.AddDays(1);
        }

        // Convert back to local time for storage and display
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunInTimeZone, timeZone);
        var nextRunLocal = nextRunUtc.ToLocalTime();

        _state.NextScheduledAnalysis = nextRunLocal;
        _logger.LogInformation("Next scheduled analysis at {Time} (configured timezone: {TimeZone})",
            nextRunLocal, timeZone.DisplayName);
    }

    private TimeZoneInfo GetConfiguredTimeZone()
    {
        if (string.IsNullOrWhiteSpace(_scheduleConfig.TimeZone))
        {
            return TimeZoneInfo.Local;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(_scheduleConfig.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("TimeZone '{TimeZone}' not found, using local timezone", _scheduleConfig.TimeZone);
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            _logger.LogWarning("TimeZone '{TimeZone}' is invalid, using local timezone", _scheduleConfig.TimeZone);
            return TimeZoneInfo.Local;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analysis Background Service stopping");

        if (_analysisTimer != null)
        {
            await _analysisTimer.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}

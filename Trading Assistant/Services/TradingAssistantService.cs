using Microsoft.Extensions.Logging;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.IPC.Messages;
using Trading_Assistant.IPC.Messages.DTOs;

namespace Trading_Assistant.Services;

public class TradingAssistantService : ITradingAssistantService, IDisposable
{
    private readonly ILogger<TradingAssistantService> _logger;
    private readonly IServiceCommandClient _client;
    private string? _uiClientId;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private DateTime? _lastKnownAnalysisTime;

    public bool IsConnected => _client.IsConnected;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<AnalysisResultDto>? AnalysisCompleted;

    public TradingAssistantService(ILogger<TradingAssistantService> logger, IServiceCommandClient client)
    {
        _logger = logger;
        _client = client;
        _client.ConnectionStateChanged += OnConnectionStateChanged;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.LogInformation("Connecting to Trading Assistant service...");
            var connected = await _client.ConnectAsync();

            if (connected)
            {
                await _client.RegisterUIAsync();
                _uiClientId = Guid.NewGuid().ToString();
                _logger.LogInformation("Connected to service and registered UI client: {ClientId}", _uiClientId);

                StartPolling();
            }
            else
            {
                _logger.LogWarning("Failed to connect to service");
            }

            return connected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to service");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            StopPolling();

            if (_client.IsConnected && _uiClientId != null)
            {
                _logger.LogInformation("Unregistering UI client: {ClientId}", _uiClientId);
                await _client.UnregisterUIAsync();
            }

            await _client.DisconnectAsync();
            _logger.LogInformation("Disconnected from service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from service");
        }
    }

    public Task<ServiceStatusDto?> GetStatusAsync() => _client.GetStatusAsync();

    public Task<AnalysisResultDto?> GetLastAnalysisAsync() => _client.GetLastAnalysisAsync();

    public Task<List<OpportunityDto>> GetOpportunitiesAsync() => _client.GetOpportunitiesAsync();

    public Task<PortfolioDto?> GetPortfolioAsync() => _client.GetPortfolioAsync();

    public Task<bool> UpdatePortfolioAsync(PortfolioDto portfolio) => _client.UpdatePortfolioAsync(portfolio);

    public Task<bool> RequestAnalysisAsync() => _client.RequestAnalysisAsync();

    private void StartPolling()
    {
        StopPolling();
        _pollCts = new CancellationTokenSource();
        _pollTask = PollForUpdatesAsync(_pollCts.Token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
        _pollTask = null;
    }

    private async Task PollForUpdatesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), ct);

                if (!_client.IsConnected) continue;

                var status = await _client.GetStatusAsync();
                if (status == null) continue;

                // Detect new analysis by comparing timestamps
                if (status.LastAnalysisTime.HasValue &&
                    status.LastAnalysisTime != _lastKnownAnalysisTime)
                {
                    _lastKnownAnalysisTime = status.LastAnalysisTime;

                    var analysis = await _client.GetLastAnalysisAsync();
                    if (analysis != null)
                    {
                        _logger.LogInformation("New analysis detected via polling with {Count} opportunities",
                            analysis.Opportunities.Count);
                        AnalysisCompleted?.Invoke(this, analysis);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during polling");
            }
        }
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        _logger.LogInformation("Connection state changed: {Connected}", connected);
        ConnectionStateChanged?.Invoke(this, connected);

        if (!connected)
        {
            StopPolling();
        }
    }

    public void Dispose()
    {
        StopPolling();
        _client.ConnectionStateChanged -= OnConnectionStateChanged;
        DisconnectAsync().GetAwaiter().GetResult();

        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

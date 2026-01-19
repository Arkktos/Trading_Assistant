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

    public bool IsConnected => _client.IsConnected;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<AnalysisResultDto>? AnalysisCompleted;

    public TradingAssistantService(ILogger<TradingAssistantService> logger, IServiceCommandClient client)
    {
        _logger = logger;
        _client = client;
        _client.ConnectionStateChanged += OnConnectionStateChanged;
        _client.EventReceived += OnServiceEvent;
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

    public async Task<bool> UpdateAssetsAsync(List<AssetDto> assets)
    {
        var dto = new UpdateAssetsCommandDto { Assets = assets };
        return await _client.UpdateAssetsAsync(dto);
    }

    public async Task<bool> UpdateTradingConfigAsync(decimal capital, string riskProfile)
    {
        var dto = new UpdateTradingConfigDto
        {
            AvailableCapital = capital,
            RiskProfile = riskProfile
        };
        return await _client.UpdateTradingConfigAsync(dto);
    }

    public Task<bool> RequestAnalysisAsync() => _client.RequestAnalysisAsync();

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        _logger.LogInformation("Connection state changed: {Connected}", connected);
        ConnectionStateChanged?.Invoke(this, connected);
    }

    private void OnServiceEvent(object? sender, ServiceEvent e)
    {
        _logger.LogDebug("Received service event: {Type}", e.Type);

        if (e.Type == EventType.AnalysisComplete && e.Payload != null)
        {
            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<AnalysisResultDto>(e.Payload);
                if (result != null)
                {
                    _logger.LogInformation("Analysis completed event received with {Count} opportunities",
                        result.Opportunities.Count);
                    AnalysisCompleted?.Invoke(this, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing analysis result from event");
            }
        }
    }

    public void Dispose()
    {
        _client.ConnectionStateChanged -= OnConnectionStateChanged;
        _client.EventReceived -= OnServiceEvent;
        DisconnectAsync().GetAwaiter().GetResult();

        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

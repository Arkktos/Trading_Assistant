using Trading_Assistant.IPC.Messages;
using Trading_Assistant.IPC.Messages.DTOs;

namespace Trading_Assistant.Contracts.Services;

public interface IServiceCommandClient : IDisposable
{
    bool IsConnected { get; }

    event EventHandler<ServiceEvent>? EventReceived;
    event EventHandler<bool>? ConnectionStateChanged;

    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();

    Task<ServiceCommandResponse> SendCommandAsync(ServiceCommand command, CancellationToken cancellationToken = default);

    // Convenience methods
    Task<ServiceStatusDto?> GetStatusAsync();
    Task<AnalysisResultDto?> GetLastAnalysisAsync();
    Task<List<OpportunityDto>> GetOpportunitiesAsync();
    Task<bool> UpdateAssetsAsync(UpdateAssetsCommandDto dto);
    Task<bool> UpdateTradingConfigAsync(UpdateTradingConfigDto dto);
    Task<bool> RequestAnalysisAsync();
    Task RegisterUIAsync();
    Task UnregisterUIAsync();
}

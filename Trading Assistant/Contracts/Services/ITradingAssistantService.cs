using Trading_Assistant.IPC.Messages.DTOs;

namespace Trading_Assistant.Contracts.Services;

public interface ITradingAssistantService
{
    bool IsConnected { get; }

    event EventHandler<bool>? ConnectionStateChanged;
    event EventHandler<AnalysisResultDto>? AnalysisCompleted;

    Task<bool> ConnectAsync();
    Task DisconnectAsync();

    Task<ServiceStatusDto?> GetStatusAsync();
    Task<AnalysisResultDto?> GetLastAnalysisAsync();
    Task<List<OpportunityDto>> GetOpportunitiesAsync();
    Task<PortfolioDto?> GetPortfolioAsync();
    Task<bool> UpdatePortfolioAsync(PortfolioDto portfolio);
    Task<bool> RequestAnalysisAsync();
}

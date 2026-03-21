using Trading_Assistant.Service.IPC.Messages.DTOs;

namespace Trading_Assistant.Service.Services;

public interface IPortfolioService
{
    Task<PortfolioDto> LoadAsync();
    Task SaveAsync(PortfolioDto portfolio);
}

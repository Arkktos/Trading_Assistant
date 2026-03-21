using Trading_Assistant.Models;

namespace Trading_Assistant.Contracts.Services;

public interface IPortfolioService
{
    Task<PortfolioData> LoadAsync();
    Task SaveAsync(PortfolioData data);
}

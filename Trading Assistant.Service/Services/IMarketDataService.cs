using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Services;

public interface IMarketDataService
{
    Task<MarketData> GetMarketDataAsync(Asset asset);
    Task<List<MarketData>> GetMarketDataForAssetsAsync(List<Asset> assets);
}

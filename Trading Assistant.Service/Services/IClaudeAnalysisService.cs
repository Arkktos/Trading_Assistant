using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Services;

public interface IClaudeAnalysisService
{
    Task<AnalysisResult> AnalyzeMarketDataAsync(List<MarketData> marketDataList, decimal availableCapital, string riskProfile);
}

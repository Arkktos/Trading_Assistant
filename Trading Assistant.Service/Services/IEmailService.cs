using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Services;

public interface IEmailService
{
    Task SendAnalysisReportAsync(AnalysisResult analysisResult, List<MarketData> marketDataList);
}

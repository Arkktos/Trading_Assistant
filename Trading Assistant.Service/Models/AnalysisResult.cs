namespace Trading_Assistant.Service.Models;

public class AnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public List<TradingOpportunity> Opportunities { get; set; } = new();
    public string RiskAssessment { get; set; } = string.Empty;
    public string MarketSentiment { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public List<string> KeyObservations { get; set; } = new();
}

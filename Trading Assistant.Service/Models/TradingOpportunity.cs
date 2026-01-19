namespace Trading_Assistant.Service.Models;

public class TradingOpportunity
{
    public Asset Asset { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "Buy", "Sell", "Hold"
    public string ConfidenceLevel { get; set; } = string.Empty; // "Low", "Medium", "High"
    public decimal? TargetPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public string Timeframe { get; set; } = string.Empty; // "Short-term", "Medium-term", "Long-term"
}

namespace Trading_Assistant.IPC.Messages.DTOs;

public class OpportunityDto
{
    public string Symbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = string.Empty;
    public decimal? TargetPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public string Timeframe { get; set; } = string.Empty;

    public string TargetPriceText => TargetPrice.HasValue ? $"${TargetPrice:F2}" : "-";
    public string StopLossText => StopLoss.HasValue ? $"${StopLoss:F2}" : "-";
}

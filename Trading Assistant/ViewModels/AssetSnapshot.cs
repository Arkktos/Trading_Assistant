using Trading_Assistant.IPC.Messages.DTOs;
using Trading_Assistant.Models;

namespace Trading_Assistant.ViewModels;

public class AssetSnapshot
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double SharesOwned { get; set; }
    public string Direction { get; set; } = "-";
    public string TargetPriceText { get; set; } = "-";
    public string StopLossText { get; set; } = "-";
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string SharesText { get; set; } = "-";
    public string EstimatedValueText { get; set; } = "-";

    public static AssetSnapshot From(AssetPosition position, OpportunityDto? opportunity)
    {
        var snap = new AssetSnapshot
        {
            Symbol = position.Symbol,
            Name = position.Name,
            SharesOwned = position.SharesOwned,
            SharesText = position.SharesOwned > 0 ? $"{position.SharesOwned:F4}" : "-",
        };

        if (opportunity != null)
        {
            snap.Direction = opportunity.Direction;
            snap.TargetPriceText = opportunity.TargetPriceText;
            snap.StopLossText = opportunity.StopLossText;
            snap.ConfidenceLevel = opportunity.ConfidenceLevel;

            if (position.SharesOwned > 0 && opportunity.TargetPrice.HasValue)
                snap.EstimatedValueText = $"~${(double)opportunity.TargetPrice.Value * position.SharesOwned:F2}";
        }

        return snap;
    }
}

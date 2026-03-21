namespace Trading_Assistant.Models;

public class PortfolioData
{
    public double AvailableCapital { get; set; }
    public string RiskProfile { get; set; } = "Moderate";
    public List<AssetPosition> Positions { get; set; } = new();
}

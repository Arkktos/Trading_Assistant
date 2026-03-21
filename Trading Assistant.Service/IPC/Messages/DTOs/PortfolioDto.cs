namespace Trading_Assistant.Service.IPC.Messages.DTOs;

public class AssetPositionDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal SharesOwned { get; set; }
}

public class PortfolioDto
{
    public decimal AvailableCapital { get; set; }
    public string RiskProfile { get; set; } = "Moderate";
    public List<AssetPositionDto> Assets { get; set; } = new();
}

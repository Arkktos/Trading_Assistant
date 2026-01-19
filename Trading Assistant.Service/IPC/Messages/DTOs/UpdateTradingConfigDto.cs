namespace Trading_Assistant.Service.IPC.Messages.DTOs;

public class UpdateTradingConfigDto
{
    public decimal AvailableCapital { get; set; }
    public string RiskProfile { get; set; } = string.Empty;
}

namespace Trading_Assistant.Service.IPC.Messages.DTOs;

public class AnalysisResultDto
{
    public string Summary { get; set; } = string.Empty;
    public List<OpportunityDto> Opportunities { get; set; } = new();
    public string RiskAssessment { get; set; } = string.Empty;
    public string MarketSentiment { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public List<string> KeyObservations { get; set; } = new();
}

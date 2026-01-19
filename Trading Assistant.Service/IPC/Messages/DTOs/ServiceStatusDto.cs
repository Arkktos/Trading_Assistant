namespace Trading_Assistant.Service.IPC.Messages.DTOs;

public class ServiceStatusDto
{
    public bool IsRunning { get; set; }
    public DateTime? LastAnalysisTime { get; set; }
    public DateTime? NextScheduledAnalysis { get; set; }
    public int RegisteredUIClients { get; set; }
    public int WatchedAssetsCount { get; set; }
    public bool IsAnalyzing { get; set; }
}

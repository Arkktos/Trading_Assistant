namespace Trading_Assistant.Service.IPC.Messages;

public enum CommandType
{
    Ping,
    GetStatus,
    GetLastAnalysis,
    GetOpportunities,
    GetPortfolio,
    UpdatePortfolio,
    RequestAnalysis,
    RegisterUI,
    UnregisterUI,
    Shutdown
}

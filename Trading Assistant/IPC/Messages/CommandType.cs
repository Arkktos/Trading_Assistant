namespace Trading_Assistant.IPC.Messages;

public enum CommandType
{
    Ping,
    GetStatus,
    GetLastAnalysis,
    GetOpportunities,
    UpdateAssets,
    UpdateTradingConfig,
    RequestAnalysis,
    RegisterUI,
    UnregisterUI,
    Shutdown
}

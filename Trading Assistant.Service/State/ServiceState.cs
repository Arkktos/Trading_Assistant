using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.State;

public class ServiceState
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _registeredUIClients = new();

    public AnalysisResult? LastAnalysis { get; private set; }
    public DateTime? LastAnalysisTime { get; private set; }
    public DateTime? NextScheduledAnalysis { get; set; }
    public bool IsAnalyzing { get; private set; }
    public int RegisteredUICount => _registeredUIClients.Count;

    public async Task SetLastAnalysisAsync(AnalysisResult result)
    {
        await _lock.WaitAsync();
        try
        {
            LastAnalysis = result;
            LastAnalysisTime = DateTime.UtcNow;
            IsAnalyzing = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetAnalyzingAsync(bool isAnalyzing)
    {
        await _lock.WaitAsync();
        try
        {
            IsAnalyzing = isAnalyzing;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RegisterUIClientAsync(string clientId)
    {
        await _lock.WaitAsync();
        try
        {
            _registeredUIClients.Add(clientId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UnregisterUIClientAsync(string clientId)
    {
        await _lock.WaitAsync();
        try
        {
            _registeredUIClients.Remove(clientId);
        }
        finally
        {
            _lock.Release();
        }
    }

}

using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.Configuration;

public class AppConfig
{
    public TradingConfig Trading { get; set; } = new();
    public ClaudeConfig Claude { get; set; } = new();
    public AlphaVantageConfig AlphaVantage { get; set; } = new();
    public ScheduleConfig Schedule { get; set; } = new();
}

public class TradingConfig
{
    public decimal AvailableCapital { get; set; }
    public string RiskProfile { get; set; } = string.Empty;
    public List<Asset> WatchedAssets { get; set; } = new();
}

public class ClaudeConfig
{
    // Configuration for Claude Code CLI
    // No API key needed - uses local Claude Code installation
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4000;
}

public class AlphaVantageConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
}

public class ScheduleConfig
{
    public string AnalysisTime { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}

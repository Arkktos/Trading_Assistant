using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.Configuration;

namespace Trading_Assistant.Service.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly string _configPath;
    private readonly ILogger<ConfigurationService> _logger;
    private AppConfig? _cachedConfig;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationService(string configPath, ILogger<ConfigurationService> logger)
    {
        _configPath = configPath;
        _logger = logger;
    }

    public AppConfig LoadConfiguration()
    {
        if (_cachedConfig != null) return _cachedConfig;

        try
        {
            var json = File.ReadAllText(_configPath);
            _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {Path}", _configPath);
            return new AppConfig();
        }
    }

    public async Task SaveConfigurationAsync(AppConfig config)
    {
        await _lock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_configPath, json);
            _cachedConfig = config;
            _logger.LogInformation("Configuration saved to {Path}", _configPath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateTradingConfigAsync(decimal capital, string riskProfile)
    {
        var config = LoadConfiguration();
        config.Trading.AvailableCapital = capital;
        config.Trading.RiskProfile = riskProfile;
        await SaveConfigurationAsync(config);
    }
}

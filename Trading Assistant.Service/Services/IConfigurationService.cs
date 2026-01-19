using Trading_Assistant.Service.Configuration;

namespace Trading_Assistant.Service.Services;

public interface IConfigurationService
{
    AppConfig LoadConfiguration();
    Task SaveConfigurationAsync(AppConfig config);
    Task UpdateTradingConfigAsync(decimal capital, string riskProfile);
}

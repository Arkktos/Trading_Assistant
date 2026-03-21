using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.IPC.Messages.DTOs;

namespace Trading_Assistant.Service.Services;

public class PortfolioService : IPortfolioService
{
    private const string FileName = "portfolio.json";
    private readonly string _folder;
    private readonly ILogger<PortfolioService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PortfolioService(ILogger<PortfolioService> logger)
    {
        _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Trading Assistant");
        _logger = logger;
    }

    public async Task<PortfolioDto> LoadAsync()
    {
        var path = Path.Combine(_folder, FileName);
        try
        {
            if (!File.Exists(path)) return new PortfolioDto();
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<PortfolioDto>(json) ?? new PortfolioDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load portfolio from {Path}", path);
            return new PortfolioDto();
        }
    }

    public async Task SaveAsync(PortfolioDto portfolio)
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, FileName);
        await _lock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(portfolio, options);
            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation("Portfolio saved: {Count} assets, capital={Capital}",
                portfolio.Assets.Count, portfolio.AvailableCapital);
        }
        finally
        {
            _lock.Release();
        }
    }
}

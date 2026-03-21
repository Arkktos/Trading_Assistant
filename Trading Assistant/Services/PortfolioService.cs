using Microsoft.Extensions.Logging;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.Models;

namespace Trading_Assistant.Services;

public class PortfolioService : IPortfolioService
{
    private const string FileName = "portfolio.json";
    private readonly string _folder;
    private readonly IFileService _fileService;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(IFileService fileService, ILogger<PortfolioService> logger)
    {
        _fileService = fileService;
        _logger = logger;
        _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Trading Assistant");
    }

    public Task<PortfolioData> LoadAsync()
    {
        try
        {
            var data = _fileService.Read<PortfolioData>(_folder, FileName);
            if (data != null)
            {
                _logger.LogInformation("Portfolio loaded: {Count} positions, capital={Capital}",
                    data.Positions.Count, data.AvailableCapital);
                return Task.FromResult(data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load portfolio, returning empty");
        }

        return Task.FromResult(new PortfolioData());
    }

    public Task SaveAsync(PortfolioData data)
    {
        try
        {
            _fileService.Save(_folder, FileName, data);
            _logger.LogInformation("Portfolio saved: {Count} positions, capital={Capital}",
                data.Positions.Count, data.AvailableCapital);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save portfolio");
            throw;
        }

        return Task.CompletedTask;
    }
}

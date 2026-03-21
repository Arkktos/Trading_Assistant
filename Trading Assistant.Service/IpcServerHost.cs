using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Trading_Assistant.Service.IPC;
using Trading_Assistant.Service.IPC.Messages;
using Trading_Assistant.Service.IPC.Messages.DTOs;
using Trading_Assistant.Service.Services;
using Trading_Assistant.Service.State;

namespace Trading_Assistant.Service;

public class IpcServerHost : IHostedService
{
    private readonly ILogger<IpcServerHost> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ServiceState _state;
    private readonly IPortfolioService _portfolioService;
    private readonly IConfigurationService _configService;
    private readonly AnalysisBackgroundService _analysisService;
    private readonly IHostApplicationLifetime _appLifetime;
    private NamedPipeServer? _pipeServer;
    private PortfolioDto _cachedPortfolio = new();

    public IpcServerHost(
        ILogger<IpcServerHost> logger,
        ILoggerFactory loggerFactory,
        ServiceState state,
        IPortfolioService portfolioService,
        IConfigurationService configService,
        AnalysisBackgroundService analysisService,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _state = state;
        _portfolioService = portfolioService;
        _configService = configService;
        _analysisService = analysisService;
        _appLifetime = appLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IPC server host");
        _cachedPortfolio = await _portfolioService.LoadAsync();
        _logger.LogInformation("Portfolio loaded: {Count} assets", _cachedPortfolio.Assets.Count);

        _pipeServer = new NamedPipeServer(_loggerFactory.CreateLogger<NamedPipeServer>(), HandleCommandAsync);
        _pipeServer.Start();
        _logger.LogInformation("IPC server started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping IPC server host");
        if (_pipeServer != null) { await _pipeServer.StopAsync(); _pipeServer.Dispose(); }
        _logger.LogInformation("IPC server stopped");
    }

    private async Task<ServiceCommandResponse> HandleCommandAsync(ServiceCommand command)
    {
        try
        {
            return command.Type switch
            {
                CommandType.Ping => new() { RequestId = command.RequestId, Success = true, Result = "pong" },
                CommandType.GetStatus => HandleGetStatus(command),
                CommandType.GetLastAnalysis => HandleGetLastAnalysis(command),
                CommandType.GetOpportunities => HandleGetOpportunities(command),
                CommandType.GetPortfolio => HandleGetPortfolio(command),
                CommandType.UpdatePortfolio => await HandleUpdatePortfolioAsync(command),
                CommandType.RequestAnalysis => HandleRequestAnalysis(command),
                CommandType.RegisterUI => await HandleRegisterUIAsync(command),
                CommandType.UnregisterUI => await HandleUnregisterUIAsync(command),
                CommandType.Shutdown => HandleShutdown(command),
                _ => new() { RequestId = command.RequestId, Success = false, ErrorMessage = "Unknown command" }
            };
        }
        catch (Exception ex)
        {
            return new() { RequestId = command.RequestId, Success = false, ErrorMessage = ex.Message };
        }
    }

    private ServiceCommandResponse HandleGetStatus(ServiceCommand command)
    {
        var status = new ServiceStatusDto
        {
            IsRunning = true, LastAnalysisTime = _state.LastAnalysisTime, NextScheduledAnalysis = _state.NextScheduledAnalysis,
            RegisteredUIClients = _state.RegisteredUICount, WatchedAssetsCount = _cachedPortfolio.Assets.Count, IsAnalyzing = _state.IsAnalyzing
        };
        return new() { RequestId = command.RequestId, Success = true, Result = JsonSerializer.Serialize(status) };
    }

    private ServiceCommandResponse HandleGetLastAnalysis(ServiceCommand command)
    {
        if (_state.LastAnalysis == null) return new() { RequestId = command.RequestId, Success = false, ErrorMessage = "No analysis yet" };
        return new() { RequestId = command.RequestId, Success = true, Result = JsonSerializer.Serialize(MapToDto(_state.LastAnalysis)) };
    }

    private ServiceCommandResponse HandleGetOpportunities(ServiceCommand command)
    {
        var opps = _state.LastAnalysis?.Opportunities.Select(o => new OpportunityDto
        {
            Symbol = o.Asset.Symbol, AssetName = o.Asset.Name, Reason = o.Reason, Direction = o.Direction,
            ConfidenceLevel = o.ConfidenceLevel, TargetPrice = o.TargetPrice, StopLoss = o.StopLoss, Timeframe = o.Timeframe
        }).ToList() ?? new();
        return new() { RequestId = command.RequestId, Success = true, Result = JsonSerializer.Serialize(opps) };
    }

    private ServiceCommandResponse HandleGetPortfolio(ServiceCommand command)
    {
        return new() { RequestId = command.RequestId, Success = true, Result = JsonSerializer.Serialize(_cachedPortfolio) };
    }

    private async Task<ServiceCommandResponse> HandleUpdatePortfolioAsync(ServiceCommand command)
    {
        var dto = JsonSerializer.Deserialize<PortfolioDto>(command.Payload ?? "");
        if (dto == null) return new() { RequestId = command.RequestId, Success = false, ErrorMessage = "Invalid payload" };

        await _portfolioService.SaveAsync(dto);
        _cachedPortfolio = dto;
        _logger.LogInformation("Portfolio updated via IPC: {Count} assets, capital={Capital}",
            dto.Assets.Count, dto.AvailableCapital);

        return new() { RequestId = command.RequestId, Success = true };
    }

    private ServiceCommandResponse HandleRequestAnalysis(ServiceCommand command)
    {
        _analysisService.RequestManualAnalysis();
        return new() { RequestId = command.RequestId, Success = true };
    }

    private ServiceCommandResponse HandleShutdown(ServiceCommand command)
    {
        _logger.LogInformation("Shutdown command received, stopping service...");
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            _appLifetime.StopApplication();
        });
        return new() { RequestId = command.RequestId, Success = true, Result = "Service shutdown initiated" };
    }

    private async Task<ServiceCommandResponse> HandleRegisterUIAsync(ServiceCommand command)
    {
        await _state.RegisterUIClientAsync(command.RequestId.ToString());
        return new() { RequestId = command.RequestId, Success = true };
    }

    private async Task<ServiceCommandResponse> HandleUnregisterUIAsync(ServiceCommand command)
    {
        await _state.UnregisterUIClientAsync(command.RequestId.ToString());
        return new() { RequestId = command.RequestId, Success = true };
    }

    private static AnalysisResultDto MapToDto(Models.AnalysisResult r) => new()
    {
        Summary = r.Summary, RiskAssessment = r.RiskAssessment, MarketSentiment = r.MarketSentiment,
        AnalysisDate = r.AnalysisDate, KeyObservations = r.KeyObservations,
        Opportunities = r.Opportunities.Select(o => new OpportunityDto
        {
            Symbol = o.Asset.Symbol, AssetName = o.Asset.Name, Reason = o.Reason, Direction = o.Direction,
            ConfidenceLevel = o.ConfidenceLevel, TargetPrice = o.TargetPrice, StopLoss = o.StopLoss, Timeframe = o.Timeframe
        }).ToList()
    };

    // Expose cached portfolio to AnalysisBackgroundService
    public PortfolioDto GetCachedPortfolio() => _cachedPortfolio;
}

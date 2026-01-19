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
    private readonly IConfigurationService _configService;
    private readonly AnalysisBackgroundService _analysisService;
    private readonly IHostApplicationLifetime _appLifetime;
    private NamedPipeServer? _pipeServer;

    public IpcServerHost(
        ILogger<IpcServerHost> logger,
        ILoggerFactory loggerFactory,
        ServiceState state,
        IConfigurationService configService,
        AnalysisBackgroundService analysisService,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _state = state;
        _configService = configService;
        _analysisService = analysisService;
        _appLifetime = appLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IPC server host");
        _pipeServer = new NamedPipeServer(_loggerFactory.CreateLogger<NamedPipeServer>(), HandleCommandAsync);
        _pipeServer.Start();
        _logger.LogInformation("IPC server started successfully");
        return Task.CompletedTask;
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
                CommandType.UpdateAssets => await HandleUpdateAssetsAsync(command),
                CommandType.UpdateTradingConfig => await HandleUpdateTradingConfigAsync(command),
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
        var config = _configService.LoadConfiguration();
        var status = new ServiceStatusDto
        {
            IsRunning = true, LastAnalysisTime = _state.LastAnalysisTime, NextScheduledAnalysis = _state.NextScheduledAnalysis,
            RegisteredUIClients = _state.RegisteredUICount, WatchedAssetsCount = config.Trading.WatchedAssets.Count, IsAnalyzing = _state.IsAnalyzing
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

    private async Task<ServiceCommandResponse> HandleUpdateAssetsAsync(ServiceCommand command)
    {
        var dto = JsonSerializer.Deserialize<UpdateAssetsCommandDto>(command.Payload ?? "");
        if (dto == null) return new() { RequestId = command.RequestId, Success = false, ErrorMessage = "Invalid payload" };
        var config = _configService.LoadConfiguration();
        config.Trading.WatchedAssets = dto.Assets;
        await _configService.SaveConfigurationAsync(config);
        return new() { RequestId = command.RequestId, Success = true };
    }

    private async Task<ServiceCommandResponse> HandleUpdateTradingConfigAsync(ServiceCommand command)
    {
        var dto = JsonSerializer.Deserialize<UpdateTradingConfigDto>(command.Payload ?? "");
        if (dto == null) return new() { RequestId = command.RequestId, Success = false, ErrorMessage = "Invalid payload" };
        await _configService.UpdateTradingConfigAsync(dto.AvailableCapital, dto.RiskProfile);
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
        // Trigger graceful shutdown on a background thread to allow response to be sent first
        _ = Task.Run(async () =>
        {
            await Task.Delay(100); // Small delay to ensure response is sent
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
}

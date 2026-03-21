using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.IPC.Messages;
using Trading_Assistant.IPC.Messages.DTOs;

namespace Trading_Assistant.IPC;

public class NamedPipeClient : IServiceCommandClient
{
    private const string PipeName = "TradingAssistantService";
    private const int ConnectTimeoutMs = 5000;

    private readonly ILogger<NamedPipeClient> _logger;
    private NamedPipeClientStream? _pipeClient;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public bool IsConnected => _pipeClient?.IsConnected ?? false;

#pragma warning disable CS0067 // Event is never used - reserved for future server-push protocol
    public event EventHandler<ServiceEvent>? EventReceived;
#pragma warning restore CS0067
    public event EventHandler<bool>? ConnectionStateChanged;

    public NamedPipeClient(ILogger<NamedPipeClient> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsConnected)
            {
                _logger.LogWarning("Already connected");
                return true;
            }

            _logger.LogInformation("Connecting to service pipe: {PipeName}", PipeName);

            _pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            using var timeoutCts = new CancellationTokenSource(ConnectTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await _pipeClient.ConnectAsync(linkedCts.Token);

            _logger.LogInformation("Connected to service successfully");
            ConnectionStateChanged?.Invoke(this, true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to service");
            _pipeClient?.Dispose();
            _pipeClient = null;
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _logger.LogInformation("Disconnecting from service");

        _pipeClient?.Dispose();
        _pipeClient = null;

        ConnectionStateChanged?.Invoke(this, false);
        await Task.CompletedTask;
    }

    public async Task<ServiceCommandResponse> SendCommandAsync(ServiceCommand command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _pipeClient == null)
        {
            return new ServiceCommandResponse
            {
                RequestId = command.RequestId,
                Success = false,
                ErrorMessage = "Not connected to service"
            };
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(command);
            var bytes = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(bytes.Length);

            await _pipeClient.WriteAsync(length, cancellationToken);
            await _pipeClient.WriteAsync(bytes, cancellationToken);
            await _pipeClient.FlushAsync(cancellationToken);

            var responseLengthBytes = new byte[4];
            await _pipeClient.ReadExactlyAsync(responseLengthBytes, cancellationToken);
            var responseLength = BitConverter.ToInt32(responseLengthBytes);

            var responseBytes = new byte[responseLength];
            await _pipeClient.ReadExactlyAsync(responseBytes, cancellationToken);

            var responseJson = Encoding.UTF8.GetString(responseBytes);
            var response = JsonSerializer.Deserialize<ServiceCommandResponse>(responseJson);

            return response ?? new ServiceCommandResponse
            {
                RequestId = command.RequestId,
                Success = false,
                ErrorMessage = "Failed to deserialize response"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command");
            return new ServiceCommandResponse
            {
                RequestId = command.RequestId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<ServiceStatusDto?> GetStatusAsync()
    {
        var command = new ServiceCommand { Type = CommandType.GetStatus };
        var response = await SendCommandAsync(command);
        return response.Success && response.Result != null
            ? JsonSerializer.Deserialize<ServiceStatusDto>(response.Result)
            : null;
    }

    public async Task<AnalysisResultDto?> GetLastAnalysisAsync()
    {
        var command = new ServiceCommand { Type = CommandType.GetLastAnalysis };
        var response = await SendCommandAsync(command);
        return response.Success && response.Result != null
            ? JsonSerializer.Deserialize<AnalysisResultDto>(response.Result)
            : null;
    }

    public async Task<List<OpportunityDto>> GetOpportunitiesAsync()
    {
        var command = new ServiceCommand { Type = CommandType.GetOpportunities };
        var response = await SendCommandAsync(command);
        return response.Success && response.Result != null
            ? JsonSerializer.Deserialize<List<OpportunityDto>>(response.Result) ?? new()
            : new();
    }

    public async Task<PortfolioDto?> GetPortfolioAsync()
    {
        var command = new ServiceCommand { Type = CommandType.GetPortfolio };
        var response = await SendCommandAsync(command);
        return response.Success && response.Result != null
            ? JsonSerializer.Deserialize<PortfolioDto>(response.Result)
            : null;
    }

    public async Task<bool> UpdatePortfolioAsync(PortfolioDto dto)
    {
        var command = new ServiceCommand
        {
            Type = CommandType.UpdatePortfolio,
            Payload = JsonSerializer.Serialize(dto)
        };
        var response = await SendCommandAsync(command);
        return response.Success;
    }

    public async Task<bool> RequestAnalysisAsync()
    {
        var command = new ServiceCommand { Type = CommandType.RequestAnalysis };
        var response = await SendCommandAsync(command);
        return response.Success;
    }

    public async Task RegisterUIAsync()
    {
        var command = new ServiceCommand { Type = CommandType.RegisterUI };
        await SendCommandAsync(command);
    }

    public async Task UnregisterUIAsync()
    {
        var command = new ServiceCommand { Type = CommandType.UnregisterUI };
        await SendCommandAsync(command);
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        _writeLock.Dispose();
    }
}

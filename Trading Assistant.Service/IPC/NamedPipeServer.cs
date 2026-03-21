using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trading_Assistant.Service.IPC.Messages;

namespace Trading_Assistant.Service.IPC;

public class NamedPipeServer : IDisposable
{
    private const string PipeName = "TradingAssistantService";
    private readonly ILogger<NamedPipeServer> _logger;
    private readonly Func<ServiceCommand, Task<ServiceCommandResponse>> _commandHandler;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private readonly List<Task> _clientTasks = new();

    public NamedPipeServer(
        ILogger<NamedPipeServer> logger,
        Func<ServiceCommand, Task<ServiceCommandResponse>> commandHandler)
    {
        _logger = logger;
        _commandHandler = commandHandler;
    }

    public void Start()
    {
        _logger.LogInformation("Starting named pipe server: {PipeName}", PipeName);
        _cts = new CancellationTokenSource();
        _listenerTask = ListenForConnectionsAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping named pipe server");
        _cts?.Cancel();

        if (_listenerTask != null)
        {
            try { await _listenerTask; } catch { }
        }

        await Task.WhenAll(_clientTasks);
        _clientTasks.Clear();
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pipeSecurity = new PipeSecurity();
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                    AccessControlType.Allow));
                pipeSecurity.AddAccessRule(new PipeAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    PipeAccessRights.FullControl,
                    AccessControlType.Allow));

                var pipeServer = NamedPipeServerStreamAcl.Create(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    inBufferSize: 0,
                    outBufferSize: 0,
                    pipeSecurity);

                _logger.LogDebug("Waiting for client connection...");
                await pipeServer.WaitForConnectionAsync(cancellationToken);
                _logger.LogInformation("Client connected");

                var clientTask = HandleClientAsync(pipeServer, cancellationToken);
                _clientTasks.Add(clientTask);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pipe server listener");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
    {
        try
        {
            using (pipeServer)
            {
                while (pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    var lengthBytes = new byte[4];
                    var bytesRead = await pipeServer.ReadAsync(lengthBytes, cancellationToken);
                    if (bytesRead == 0) break;

                    var length = BitConverter.ToInt32(lengthBytes);
                    var commandBytes = new byte[length];
                    await pipeServer.ReadExactlyAsync(commandBytes, cancellationToken);

                    var commandJson = Encoding.UTF8.GetString(commandBytes);
                    var command = JsonSerializer.Deserialize<ServiceCommand>(commandJson);

                    if (command != null)
                    {
                        _logger.LogDebug("Received command: {Type}", command.Type);
                        var response = await _commandHandler(command);

                        var responseJson = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                        var responseLengthBytes = BitConverter.GetBytes(responseBytes.Length);

                        await pipeServer.WriteAsync(responseLengthBytes, cancellationToken);
                        await pipeServer.WriteAsync(responseBytes, cancellationToken);
                        await pipeServer.FlushAsync(cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client");
        }
        finally
        {
            _logger.LogInformation("Client disconnected");
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
    }
}

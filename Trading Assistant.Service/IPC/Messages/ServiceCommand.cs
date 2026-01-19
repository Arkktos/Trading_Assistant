namespace Trading_Assistant.Service.IPC.Messages;

public class ServiceCommand
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public CommandType Type { get; set; }
    public string? Payload { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ServiceCommandResponse
{
    public Guid RequestId { get; set; }
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

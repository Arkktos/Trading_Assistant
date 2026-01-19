namespace Trading_Assistant.IPC.Messages;

public class ServiceEvent
{
    public EventType Type { get; set; }
    public string? Payload { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

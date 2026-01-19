using Trading_Assistant.Service.Models;

namespace Trading_Assistant.Service.IPC.Messages.DTOs;

public class UpdateAssetsCommandDto
{
    public List<Asset> Assets { get; set; } = new();
}

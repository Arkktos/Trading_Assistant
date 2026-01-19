namespace Trading_Assistant.IPC.Messages.DTOs;

public class AssetDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class UpdateAssetsCommandDto
{
    public List<AssetDto> Assets { get; set; } = new();
}

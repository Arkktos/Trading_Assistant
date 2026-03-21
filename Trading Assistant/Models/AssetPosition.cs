using CommunityToolkit.Mvvm.ComponentModel;

namespace Trading_Assistant.Models;

public partial class AssetPosition : ObservableObject
{
    [ObservableProperty]
    public partial string Symbol { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Type { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SharesOwned { get; set; }
}

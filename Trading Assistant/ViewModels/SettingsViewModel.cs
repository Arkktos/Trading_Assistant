using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.IPC.Messages.DTOs;

namespace Trading_Assistant.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ITradingAssistantService _tradingService;
    //private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private decimal _availableCapital;

    [ObservableProperty]
    private string _riskProfile = "Moderate";

    [ObservableProperty]
    private int _selectedRiskProfileIndex = 1;

    [ObservableProperty]
    private ObservableCollection<AssetDto> _watchedAssets = new();

    // New asset form
    [ObservableProperty]
    private string _newAssetSymbol = string.Empty;

    [ObservableProperty]
    private string _newAssetName = string.Empty;

    [ObservableProperty]
    private int _selectedAssetTypeIndex = 0;

    public ObservableCollection<string> RiskProfiles { get; } = new()
    {
        "Conservative",
        "Moderate",
        "Aggressive"
    };

    public ObservableCollection<string> AssetTypes { get; } = new()
    {
        "Stock",
        "ETF",
        "Index",
        "Crypto"
    };

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ITradingAssistantService tradingService)
        //INotificationService notificationService)
    {
        _logger = logger;
        _tradingService = tradingService;
        //_notificationService = notificationService;
    }

    public async Task LoadSettingsAsync()
    {
        if (!_tradingService.IsConnected)
        {
            StatusMessage = "Not connected to service";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Loading settings...";

            var status = await _tradingService.GetStatusAsync();
            if (status != null)
            {
                // We need to get the full configuration from service
                // For now, just show status info
                StatusMessage = $"Watching {status.WatchedAssetsCount} asset(s)";
            }

            _logger.LogInformation("Settings loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveTradingConfigAsync()
    {
        if (!_tradingService.IsConnected)
        {
            //_notificationService.ShowErrorNotification("Not connected to service");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Saving trading configuration...";

            var riskProfile = RiskProfiles[SelectedRiskProfileIndex];
            var success = await _tradingService.UpdateTradingConfigAsync(AvailableCapital, riskProfile);

            if (success)
            {
                StatusMessage = "Trading configuration saved!";
                _logger.LogInformation("Trading config saved: Capital={Capital}, Risk={Risk}",
                    AvailableCapital, riskProfile);
            }
            else
            {
                StatusMessage = "Failed to save configuration";
                //_notificationService.ShowErrorNotification("Failed to save configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving trading config");
            StatusMessage = $"Error: {ex.Message}";
            //_notificationService.ShowErrorNotification(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAssetSymbol) || string.IsNullOrWhiteSpace(NewAssetName))
        {
            StatusMessage = "Please enter symbol and name";
            return;
        }

        if (!_tradingService.IsConnected)
        {
            //_notificationService.ShowErrorNotification("Not connected to service");
            return;
        }

        try
        {
            IsLoading = true;

            var newAsset = new AssetDto
            {
                Symbol = NewAssetSymbol.ToUpper().Trim(),
                Name = NewAssetName.Trim(),
                Type = AssetTypes[SelectedAssetTypeIndex]
            };

            // Check for duplicates
            if (WatchedAssets.Any(a => a.Symbol.Equals(newAsset.Symbol, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = $"Asset {newAsset.Symbol} already exists";
                return;
            }

            WatchedAssets.Add(newAsset);
            await SaveAssetsAsync();

            // Clear form
            NewAssetSymbol = string.Empty;
            NewAssetName = string.Empty;
            SelectedAssetTypeIndex = 0;

            StatusMessage = $"Added {newAsset.Symbol}";
            _logger.LogInformation("Added asset: {Symbol}", newAsset.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding asset");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RemoveAssetAsync(AssetDto? asset)
    {
        if (asset == null) return;

        if (!_tradingService.IsConnected)
        {
            //_notificationService.ShowErrorNotification("Not connected to service");
            return;
        }

        try
        {
            IsLoading = true;

            WatchedAssets.Remove(asset);
            await SaveAssetsAsync();

            StatusMessage = $"Removed {asset.Symbol}";
            _logger.LogInformation("Removed asset: {Symbol}", asset.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing asset");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAssetsAsync()
    {
        var success = await _tradingService.UpdateAssetsAsync(WatchedAssets.ToList());
        if (!success)
        {
            //_notificationService.ShowErrorNotification("Failed to save assets");
        }
    }

    partial void OnSelectedRiskProfileIndexChanged(int value)
    {
        if (value >= 0 && value < RiskProfiles.Count)
        {
            RiskProfile = RiskProfiles[value];
        }
    }
}

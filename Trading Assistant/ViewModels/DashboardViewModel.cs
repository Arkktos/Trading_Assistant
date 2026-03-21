using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.IPC.Messages.DTOs;
using Trading_Assistant.Models;

namespace Trading_Assistant.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly ITradingAssistantService _tradingService;
    private readonly INotificationService _notificationService;

    // ── Service status ────────────────────────────────────────────────────────

    [ObservableProperty]
    public partial bool IsConnected { get; set; }

    [ObservableProperty]
    public partial bool IsAnalyzing { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Connecting to service...";

    // ── Analysis results ──────────────────────────────────────────────────────

    [ObservableProperty]
    public partial string? Summary { get; set; }

    [ObservableProperty]
    public partial string? RiskAssessment { get; set; }

    [ObservableProperty]
    public partial string? MarketSentiment { get; set; }

    [ObservableProperty]
    public partial DateTime? LastAnalysisDate { get; set; }

    [ObservableProperty]
    public partial DateTime? NextScheduledAnalysis { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<OpportunityDto> Opportunities { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<string> KeyObservations { get; set; } = new();

    // ── Portfolio config ──────────────────────────────────────────────────────

    [ObservableProperty]
    public partial double AvailableCapital { get; set; }

    [ObservableProperty]
    public partial int SelectedRiskProfileIndex { get; set; } = 1; // Moderate

    [ObservableProperty]
    public partial ObservableCollection<AssetPosition> WatchedAssets { get; set; } = new();

    // ── Add asset form ────────────────────────────────────────────────────────

    [ObservableProperty]
    public partial string NewAssetSymbol { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewAssetName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedAssetTypeIndex { get; set; } = 0;

    // ── Portfolio snapshot (right panel) ──────────────────────────────────────

    [ObservableProperty]
    public partial ObservableCollection<AssetSnapshot> PortfolioSnapshot { get; set; } = new();

    [ObservableProperty]
    public partial string PortfolioValueText { get; set; } = string.Empty;

    private AnalysisResultDto? _lastAnalysis;

    private static readonly string[] RiskProfiles = { "Conservative", "Moderate", "Aggressive" };
    private static readonly string[] AssetTypes = { "Stock", "ETF", "Crypto", "Forex" };

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        ITradingAssistantService tradingService,
        INotificationService notificationService)
    {
        _logger = logger;
        _tradingService = tradingService;
        _notificationService = notificationService;

        _tradingService.ConnectionStateChanged += OnConnectionStateChanged;
        _tradingService.AnalysisCompleted += OnAnalysisCompleted;
    }

    public async Task InitializeAsync()
    {
        await ConnectToServiceAsync();
        if (IsConnected)
        {
            await RefreshDataAsync();
            await LoadPortfolioAsync();
        }
    }

    // ── Service commands ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ConnectToServiceAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Connecting to service...";

            var connected = await _tradingService.ConnectAsync();
            IsConnected = connected;
            StatusMessage = connected ? "Connected" : "Failed to connect. Is the service running?";

            if (connected)
                _logger.LogInformation("Successfully connected to service");
            else
                _logger.LogWarning("Failed to connect to service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to service");
            StatusMessage = $"Connection error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        if (!IsConnected) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing data...";

            var status = await _tradingService.GetStatusAsync();
            if (status != null)
            {
                IsAnalyzing = status.IsAnalyzing;
                NextScheduledAnalysis = status.NextScheduledAnalysis;
            }

            var analysis = await _tradingService.GetLastAnalysisAsync();
            if (analysis != null)
            {
                UpdateFromAnalysis(analysis);
                StatusMessage = $"Last analysis: {analysis.AnalysisDate:g}";
            }
            else
            {
                StatusMessage = "No analysis available yet";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing data");
            StatusMessage = $"Refresh error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RequestAnalysisAsync()
    {
        if (!IsConnected)
        {
            _notificationService.ShowError("Not connected to service");
            return;
        }

        if (IsAnalyzing)
        {
            _notificationService.ShowError("Analysis already in progress");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Requesting analysis...";

            var success = await _tradingService.RequestAnalysisAsync();
            if (success)
            {
                IsAnalyzing = true;
                StatusMessage = "Analysis started...";
                _notificationService.ShowInfo("Analysis started", "A new market analysis has been requested.");
            }
            else
            {
                StatusMessage = "Failed to start analysis";
                _notificationService.ShowError("Failed to start analysis");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting analysis");
            StatusMessage = $"Error: {ex.Message}";
            _notificationService.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Portfolio commands ────────────────────────────────────────────────────

    [RelayCommand]
    private void AddAsset()
    {
        var symbol = NewAssetSymbol.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(symbol)) return;

        if (WatchedAssets.Any(a => a.Symbol == symbol))
        {
            _notificationService.ShowError($"Asset {symbol} is already in the list");
            return;
        }

        var type = SelectedAssetTypeIndex >= 0 && SelectedAssetTypeIndex < AssetTypes.Length
            ? AssetTypes[SelectedAssetTypeIndex]
            : "Stock";

        WatchedAssets.Add(new AssetPosition
        {
            Symbol = symbol,
            Name = NewAssetName.Trim(),
            Type = type,
            SharesOwned = 0
        });

        NewAssetSymbol = string.Empty;
        NewAssetName = string.Empty;
    }

    [RelayCommand]
    private void RemoveAsset(AssetPosition asset)
    {
        WatchedAssets.Remove(asset);
        RebuildSnapshot();
    }

    [RelayCommand]
    private async Task SavePortfolioAsync()
    {
        if (!IsConnected)
        {
            _notificationService.ShowError("Not connected to service");
            return;
        }

        try
        {
            IsLoading = true;
            var dto = BuildPortfolioDto();
            var success = await _tradingService.UpdatePortfolioAsync(dto);
            if (success)
            {
                _notificationService.ShowInfo("Portfolio saved", "Your portfolio configuration has been saved.");
                RebuildSnapshot();
            }
            else
            {
                _notificationService.ShowError("Failed to save portfolio");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving portfolio");
            _notificationService.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Portfolio helpers ─────────────────────────────────────────────────────

    private async Task LoadPortfolioAsync()
    {
        try
        {
            var dto = await _tradingService.GetPortfolioAsync();
            if (dto != null)
                ApplyPortfolio(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading portfolio");
        }
    }

    private void ApplyPortfolio(PortfolioDto dto)
    {
        AvailableCapital = (double)dto.AvailableCapital;

        var idx = Array.IndexOf(RiskProfiles, dto.RiskProfile);
        SelectedRiskProfileIndex = idx >= 0 ? idx : 1;

        WatchedAssets.Clear();
        foreach (var a in dto.Assets)
        {
            WatchedAssets.Add(new AssetPosition
            {
                Symbol = a.Symbol,
                Name = a.Name,
                Type = a.Type,
                SharesOwned = (double)a.SharesOwned
            });
        }

        RebuildSnapshot();
    }

    private PortfolioDto BuildPortfolioDto()
    {
        var riskProfile = SelectedRiskProfileIndex >= 0 && SelectedRiskProfileIndex < RiskProfiles.Length
            ? RiskProfiles[SelectedRiskProfileIndex]
            : "Moderate";

        return new PortfolioDto
        {
            AvailableCapital = (decimal)AvailableCapital,
            RiskProfile = riskProfile,
            Assets = WatchedAssets.Select(a => new AssetPositionDto
            {
                Symbol = a.Symbol,
                Name = a.Name,
                Type = a.Type,
                SharesOwned = (decimal)a.SharesOwned
            }).ToList()
        };
    }

    private void RebuildSnapshot()
    {
        PortfolioSnapshot.Clear();

        double totalValue = 0;
        foreach (var asset in WatchedAssets.Where(a => a.SharesOwned > 0))
        {
            var opp = _lastAnalysis?.Opportunities.FirstOrDefault(o => o.Symbol == asset.Symbol);
            var snap = AssetSnapshot.From(asset, opp);
            PortfolioSnapshot.Add(snap);

            if (opp?.TargetPrice.HasValue == true)
                totalValue += (double)opp.TargetPrice.Value * asset.SharesOwned;
        }

        PortfolioValueText = totalValue > 0 ? $"~${totalValue:F2}" : string.Empty;
    }

    // ── Analysis update ───────────────────────────────────────────────────────

    private void UpdateFromAnalysis(AnalysisResultDto analysis)
    {
        _lastAnalysis = analysis;
        Summary = analysis.Summary;
        RiskAssessment = analysis.RiskAssessment;
        MarketSentiment = analysis.MarketSentiment;
        LastAnalysisDate = analysis.AnalysisDate;

        Opportunities.Clear();
        foreach (var opp in analysis.Opportunities)
            Opportunities.Add(opp);

        KeyObservations.Clear();
        foreach (var obs in analysis.KeyObservations)
            KeyObservations.Add(obs);

        RebuildSnapshot();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        IsConnected = connected;
        StatusMessage = connected ? "Connected" : "Disconnected";

        if (!connected)
            _notificationService.ShowWarning("Service disconnected", "Lost connection to the Trading Assistant service.");
    }

    private void OnAnalysisCompleted(object? sender, AnalysisResultDto result)
    {
        _logger.LogInformation("Analysis completed: {Count} opportunities", result.Opportunities.Count);

        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            UpdateFromAnalysis(result);
            IsAnalyzing = false;
            StatusMessage = $"Analysis complete - {result.Opportunities.Count} opportunity(ies) found";

            if (result.Opportunities.Count > 0)
                _notificationService.ShowOpportunities(result.Opportunities.Count);
            else
                _notificationService.ShowInfo("Analysis complete", "No new opportunities detected.");
        });
    }
}

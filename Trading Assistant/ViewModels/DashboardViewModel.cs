using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.IPC.Messages.DTOs;

namespace Trading_Assistant.ViewModels;

public partial class DashboardViewModel : ObservableRecipient
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly ITradingAssistantService _tradingService;
    //private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Connecting to service...";

    [ObservableProperty]
    private string? _summary;

    [ObservableProperty]
    private string? _riskAssessment;

    [ObservableProperty]
    private string? _marketSentiment;

    [ObservableProperty]
    private DateTime? _lastAnalysisDate;

    [ObservableProperty]
    private DateTime? _nextScheduledAnalysis;

    [ObservableProperty]
    private int _watchedAssetsCount;

    [ObservableProperty]
    private ObservableCollection<OpportunityDto> _opportunities = new();

    [ObservableProperty]
    private ObservableCollection<string> _keyObservations = new();

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        ITradingAssistantService tradingService)
        //INotificationService notificationService)
    {
        _logger = logger;
        _tradingService = tradingService;
        //_notificationService = notificationService;

        _tradingService.ConnectionStateChanged += OnConnectionStateChanged;
        _tradingService.AnalysisCompleted += OnAnalysisCompleted;
    }

    public async Task InitializeAsync()
    {
        await ConnectToServiceAsync();
        if (IsConnected)
        {
            await RefreshDataAsync();
        }
    }

    [RelayCommand]
    private async Task ConnectToServiceAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Connecting to service...";

            var connected = await _tradingService.ConnectAsync();
            IsConnected = connected;

            if (connected)
            {
                StatusMessage = "Connected";
                _logger.LogInformation("Successfully connected to service");
            }
            else
            {
                StatusMessage = "Failed to connect. Is the service running?";
                _logger.LogWarning("Failed to connect to service");
            }
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
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot refresh data - not connected");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Refreshing data...";

            // Get service status
            var status = await _tradingService.GetStatusAsync();
            if (status != null)
            {
                IsAnalyzing = status.IsAnalyzing;
                NextScheduledAnalysis = status.NextScheduledAnalysis;
                WatchedAssetsCount = status.WatchedAssetsCount;
            }

            // Get last analysis
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

            _logger.LogInformation("Data refreshed successfully");
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
            //_notificationService.ShowErrorNotification("Not connected to service");
            return;
        }

        if (IsAnalyzing)
        {
            //_notificationService.ShowErrorNotification("Analysis already in progress");
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
                //_notificationService.ShowAnalysisStartedNotification();
                _logger.LogInformation("Analysis requested successfully");
            }
            else
            {
                StatusMessage = "Failed to start analysis";
                //_notificationService.ShowErrorNotification("Failed to start analysis");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting analysis");
            StatusMessage = $"Error: {ex.Message}";
            //_notificationService.ShowErrorNotification(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateFromAnalysis(AnalysisResultDto analysis)
    {
        Summary = analysis.Summary;
        RiskAssessment = analysis.RiskAssessment;
        MarketSentiment = analysis.MarketSentiment;
        LastAnalysisDate = analysis.AnalysisDate;

        Opportunities.Clear();
        foreach (var opp in analysis.Opportunities)
        {
            Opportunities.Add(opp);
        }

        KeyObservations.Clear();
        foreach (var obs in analysis.KeyObservations)
        {
            KeyObservations.Add(obs);
        }
    }

    private void OnConnectionStateChanged(object? sender, bool connected)
    {
        IsConnected = connected;
        StatusMessage = connected ? "Connected" : "Disconnected";

        if (!connected)
        {
            //_notificationService.ShowConnectionNotification(false);
        }
    }

    private void OnAnalysisCompleted(object? sender, AnalysisResultDto result)
    {
        _logger.LogInformation("Analysis completed event received with {Count} opportunities",
            result.Opportunities.Count);

        // Update UI on dispatcher thread
        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
        {
            UpdateFromAnalysis(result);
            IsAnalyzing = false;
            StatusMessage = $"Analysis complete - {result.Opportunities.Count} opportunity(ies) found";

            if (result.Opportunities.Count > 0)
            {
                //_notificationService.ShowOpportunitiesNotification(result.Opportunities.Count);
            }
            else
            {
                //_notificationService.ShowAnalysisCompletedNotification(0);
            }
        });
    }
}

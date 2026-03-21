using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Trading_Assistant.Contracts.Services;

namespace Trading_Assistant.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowInfo(string title, string message)
    {
        Show(title, message, "informational");
    }

    public void ShowWarning(string title, string message)
    {
        Show(title, message, "urgent");
    }

    public void ShowError(string message)
    {
        Show("Error", message, "urgent");
    }

    public void ShowOpportunities(int count)
    {
        var title = $"{count} Trading Opportunit{(count == 1 ? "y" : "ies")} Detected";
        var message = "Open Trading Assistant to review the opportunities.";
        Show(title, message, "reminder");
    }

    public void ShowAnalysisComplete(string summary)
    {
        Show("Analysis Complete", summary, "informational");
    }

    private void Show(string title, string message, string scenario)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .SetScenario(scenario switch
                {
                    "urgent" => AppNotificationScenario.Urgent,
                    "reminder" => AppNotificationScenario.Reminder,
                    _ => AppNotificationScenario.Default
                });

            var notification = builder.BuildNotification();
            AppNotificationManager.Default.Show(notification);

            _logger.LogDebug("Notification shown: {Title}", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show notification: {Title}", title);
        }
    }
}

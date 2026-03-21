namespace Trading_Assistant.Contracts.Services;

public interface INotificationService
{
    void ShowInfo(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string message);
    void ShowOpportunities(int count);
    void ShowAnalysisComplete(string summary);
}

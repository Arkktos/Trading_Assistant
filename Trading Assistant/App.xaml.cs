using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Trading_Assistant.Contracts.Services;
using Trading_Assistant.IPC;
using Trading_Assistant.Services;
using Trading_Assistant.ViewModels;
using Trading_Assistant.Views;

namespace Trading_Assistant;

public partial class App : Application
{
    public IHost Host { get; }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                // Core services
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IPortfolioService, PortfolioService>();

                // IPC
                services.AddSingleton<IServiceCommandClient>(sp =>
                    new NamedPipeClient(sp.GetRequiredService<ILogger<NamedPipeClient>>()));
                services.AddSingleton<ITradingAssistantService, TradingAssistantService>();

                // ViewModels
                services.AddTransient<DashboardViewModel>();
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Register toast notifications
        AppNotificationManager.Default.Register();

        MainWindow.Content = new DashboardPage();
        MainWindow.Activate();
    }
}

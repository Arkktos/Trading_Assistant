using Microsoft.UI.Xaml.Controls;
using Trading_Assistant.Contracts.Services;

namespace Trading_Assistant;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Title = "Trading Assistant";

        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // Enable system tray icon
        IsVisibleInTray = true;

        WindowManager.Get(this).TrayIconSelected += (s, e) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                this.Show();
                this.BringToFront();
            });
        };

        WindowManager.Get(this).TrayIconContextMenu += (s, e) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var flyout = new MenuFlyout();

                var showItem = new MenuFlyoutItem { Text = "Show" };
                showItem.Click += (_, _) =>
                {
                    this.Show();
                    this.BringToFront();
                };
                flyout.Items.Add(showItem);
                flyout.Items.Add(new MenuFlyoutSeparator());

                var exitItem = new MenuFlyoutItem { Text = "Exit" };
                exitItem.Click += (_, _) =>
                {
                    _isClosing = true;
                    Close();
                };
                flyout.Items.Add(exitItem);

                e.Flyout = flyout;
            });
        };
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_isClosing)
        {
            args.Handled = true;
            this.Hide();
            return;
        }

        try
        {
            _ = App.GetService<ITradingAssistantService>().DisconnectAsync();
        }
        catch
        {
            // Service not available during startup
        }
    }
}

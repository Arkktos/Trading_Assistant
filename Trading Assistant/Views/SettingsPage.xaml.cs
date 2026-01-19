using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Trading_Assistant.IPC.Messages.DTOs;
using Trading_Assistant.ViewModels;

namespace Trading_Assistant.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadSettingsAsync();
    }

    private void OnRemoveAssetClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AssetDto asset)
        {
            ViewModel.RemoveAssetCommand.Execute(asset);
        }
    }
}

using Microsoft.UI.Xaml.Controls;

using Trading_Assistant.ViewModels;

namespace Trading_Assistant.Views;

public sealed partial class BlankPage : Page
{
    public BlankViewModel ViewModel
    {
        get;
    }

    public BlankPage()
    {
        ViewModel = App.GetService<BlankViewModel>();
        InitializeComponent();
    }
}

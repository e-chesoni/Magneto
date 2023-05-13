using CommunityToolkit.WinUI.UI.Controls;

using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintQueuePage : Page
{
    public PrintQueueViewModel ViewModel
    {
        get;
    }

    public PrintQueuePage()
    {
        ViewModel = App.GetService<PrintQueueViewModel>();
        InitializeComponent();
    }

    private void OnViewStateChanged(object sender, ListDetailsViewState e)
    {
        if (e == ListDetailsViewState.Both)
        {
            ViewModel.EnsureItemSelected();
        }
    }
}

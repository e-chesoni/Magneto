using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class CompletedPrintsPage : Page
{
    public CompletedPrintsViewModel ViewModel
    {
        get;
    }

    public CompletedPrintsPage()
    {
        ViewModel = App.GetService<CompletedPrintsViewModel>();
        InitializeComponent();
    }
}

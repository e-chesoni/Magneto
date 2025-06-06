using System.Diagnostics;
using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using Magneto.Desktop.WinUI.Core.Services.Database.Seeders;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class PrintingHistoryPage : Page
{
    public PrintingHistoryViewModel ViewModel { get; }
    private IMongoDbSeeder _mongoDbSeeder;
    private MissionControl? _missionControl { get; set; }

    #region Constructor

    /// <summary>
    /// Completed Print Page constructor
    /// </summary>
    public PrintingHistoryPage()
    {
        ViewModel = App.GetService<PrintingHistoryViewModel>();
        _mongoDbSeeder = App.GetService<IMongoDbSeeder>();
        _missionControl = App.GetService<MissionControl>();
        InitializeComponent();
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);
    }

    #endregion

    private async void ClearDatabaseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var confirmDialog = new ContentDialog
        {
            Title = "Confirm Database Clear",
            Content = "This will delete all print history from the database. Are you sure?",
            PrimaryButtonText = "Delete All",
            CloseButtonText = "Cancel",
            XamlRoot = this.Content.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _mongoDbSeeder.ClearDatabaseAsync(true); // ✅ Clear db in mongo
            ViewModel.printCollection.Clear(); // ✅ Clear UI
        }
    }

    private async void DeletePrintButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Debug.WriteLine("Delete print button clicked.");

        if (sender is Button button && button.Tag is string id)
        {
            Debug.WriteLine($"[DeleteButton_Click] Delete requested for ID: {id}");

            // Fetch the print first to get its name
            PrintModel printModel = await ViewModel.GetPrintByIdAsync(id);

            if (printModel == null)
            {
                Debug.WriteLine($"[DeleteButton_Click] No print found with ID: {id}");
                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Delete Print?",
                Content = $"Are you sure you want to delete: \"{printModel.name}\"?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeletePrintAsync(printModel);
            }
        }
        else
        {
            Debug.WriteLine("[DeleteButton_Click] Could not retrieve ID from button.Tag.");
        }
    }

}

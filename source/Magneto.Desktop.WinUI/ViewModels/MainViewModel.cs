using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Contracts.ViewModels;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motors;

namespace Magneto.Desktop.WinUI.ViewModels;

public class MainViewModel : ObservableRecipient, INavigationAware
{
    #region Navigation and Data Variables

    public INavigationService _navigationService;
    #endregion

    /// <summary>
    /// Tasks to handle when application starts up
    /// TODO: May want to store in an "App Startup" class in the future
    /// </summary>
    private void InitializeMagnetoPorts()
    {
        // Set log level
        MagnetoLogger.LogFactoryOutputLevel = LogFactoryOutputLevel.LogOutputLevel.Debug;

        MagnetoSerialConsole.LogAvailablePorts();

        // Get config stuff
        foreach (var c in MagnetoConfig.GetAllCOMPorts())
        {
            MagnetoSerialConsole.InitializePort(MagnetoConfig.GetCOMPortName(c), c.baudRate, c.parity, c.dataBits, c.stopBits, c.handshake);
        }

        MagnetoSerialConsole.GetInitializedPorts();

        // Set default termread value
        MagnetoSerialConsole.ClearTermRead();
    }

    public ICommand ItemClickCommand
    {
        get;
    }

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        ItemClickCommand = new RelayCommand<SampleOrder>(OnItemClick);
        InitializeMagnetoPorts();

    }

    public async void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {
    }

    private void OnItemClick(SampleOrder? clickedItem)
    {
        if (clickedItem != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(clickedItem);
            _navigationService.NavigateTo(typeof(MainDetailViewModel).FullName!, clickedItem.OrderID);
        }
    }
}

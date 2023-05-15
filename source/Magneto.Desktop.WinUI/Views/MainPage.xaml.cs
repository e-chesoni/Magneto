using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using WinUIEx.Messaging;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MainPage : Page
{
    //MagnetoLogger _magnetoLogger = new MagnetoLogger();

    private void InitializeMagneto()
    {
        MagnetoSerialConsole.SetDefaultSerialPort();
    }

    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        InitializeMagneto();

        // Print some log messages for testing
        MagnetoLogger.Log("PRINTING SAMPLE LOG MESSAGES", MagnetoLogger.logLevels.VERBOSE);
        MagnetoLogger.Log("This is a debug message", MagnetoLogger.logLevels.DEBUG);
        MagnetoLogger.Log("This is a verbose message", MagnetoLogger.logLevels.VERBOSE);
        MagnetoLogger.Log("This is a warning message", MagnetoLogger.logLevels.WARN);
        MagnetoLogger.Log("This is a error message", MagnetoLogger.logLevels.ERROR);
        MagnetoLogger.Log("This is a success message", MagnetoLogger.logLevels.SUCCESS);

    }
}

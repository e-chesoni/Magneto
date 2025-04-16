using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Popups;
using Microsoft.UI.Xaml;
using SAMLIGHT_CLIENT_CTRL_EXLib;

namespace Magneto.Desktop.WinUI.Services;
public class WaverunerService : IWaverunnerService
{
    private static readonly ScSamlightClientCtrlEx cci = new();

    public WaverunerService()
    {
            
    }

    public int TestConnection()
    {
        try
        {
            // Show hello world message box in SAMlight
            cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandTest);
            return 1;
        }
        catch (System.Exception exception)
        {
            // TODO: Use Log & Display once it's extrapolated from TestPrintPage.xaml.cs
            var logMsg = $"CCI Error! \n {Convert.ToString(exception)}";
            var displayMsg = "Unable to say hello to waverunner. Is the application open?";
            MagnetoLogger.Log(logMsg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
    }
    public int StartRedPointer(string filePath)
    {
        if (cci.ScIsRunning() == 0)
        {
            return 0;
        }
        // load demo job file
        cci.ScLoadJob(filePath, 1, 1, 0);
        // returns void
        cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandRedPointerStart);
        // TODO: Replace once we figure out how to interact with error codes form SAM
        return 1;
    }
    public int StopRedPointer()
    {
        // returns void
        cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandRedPointerStop);
        // make sure laser does not mark when stopping red pointer
        cci.ScStopMarking();
        // TODO: Replace once we figure out how to interact with error codes form SAM
        return 1;
    }
    public async Task<int> MarkEntityAsync(string filePath)
    {
        // File exists, proceed with marking
        var msg = $"Starting mark for file: {filePath}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        if (cci.ScIsRunning() == 0)
        {
            return 0;
        }
        // load demo job file
        cci.ScLoadJob(filePath, 1, 1, 0);
        msg = $"Loaded file at path: {filePath} for marking...";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        try
        {
            cci.ScMarkEntityByName("", 0); // 0 returns control to the user immediately; if you use 1, this becomes a blocking function
            // Wait for marking to complete
            while (cci.ScIsMarking() != 0)
            {
                await Task.Delay(100); // Use a delay to throttle the loop for checking marking status
            }
            cci.ScStopMarking();
            return 1;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return 0;
        }
    }
    public int GetMarkStatus()
    {
        return cci.ScIsMarking();
    }
    public int StopMark()
    {
        var msg = "";
        if (cci.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        msg = "SAMLight is stopping mark";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        cci.ScStopMarking();
        return 1;
    }
}

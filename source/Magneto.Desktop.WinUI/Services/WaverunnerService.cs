using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Popups;
using Microsoft.UI.Xaml;
using SAMLIGHT_CLIENT_CTRL_EXLib;
using SharpCompress.Common;

namespace Magneto.Desktop.WinUI.Services;
public class WaverunnerService : IWaverunnerService
{
    private static readonly ScSamlightClientCtrlEx cci = new();
    private double _defaultMarkSpeed = 800; // mm/s
    private double _defaultLaserPower = 300; // W
    private double _defaultHatchSpacing = 0.12;
    private double _defaultSupplyAmplifier = 2;
    /// <summary>
    /// RedPointer Modes
    /// </summary>
    public enum RedPointerMode
    {
        IndividualOutline = 1,
        TotalOutline = 2,
        IndividualBorder = 3,
        OnlyRedPointerEntities = 4,
        OutermostBorder = 5
    }
    #region Constructor
    public WaverunnerService() {}
    #endregion

    #region Helpers
    //TODO: move to page service
    public double CalculateEnergyDensity(double layerThickness, double power, double scanSpeed, double hatchSpacing) => power / (layerThickness * scanSpeed * hatchSpacing);
    #endregion

    #region Connection Checkers
    public bool IsRunning() => cci.ScIsRunning() == 1; // 0 if not running, 1 if running

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
            MagnetoLogger.Log(logMsg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
    }
    #endregion

    #region Pen Methods
    #region Pen Getters
    public int GetPenNumber(string entityName)
    {
        return cci.ScGetEntityLongData(entityName, (int)ScComSAMLightClientCtrlFlags.scComSAMLightClientCtrlLongDataIdEntityGetPen);
    }
    public double GetMarkSpeed() // mm per second
    {
        return cci.ScGetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeMarkSpeed);
    }
    // Get laser power
    public double GetLaserPower() // Watts
    {
        return cci.ScGetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeLaserPower);
    }
    public double GetDefaultMarkSpeed() => _defaultMarkSpeed;
    public double GetDefaultLaserPower() => _defaultLaserPower;
    public double GetDefaultHatchSpacing() => _defaultHatchSpacing;
    public double GetDefaultSupplyAmplifier() => _defaultSupplyAmplifier;
    #endregion

    #region Pen Setters
    //TODO: Figure out how to implement error checking with these void commands
    // Set mark speed
    public void SetMarkSpeed(double markSpeed) // mm per second
    {
       cci.ScSetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeMarkSpeed, markSpeed); // returns void
    }
    // Set laser power
    public void SetLaserPower(double power) // Watts
    {
        cci.ScSetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeLaserPower, power); // returns void
    }
    #endregion
    #endregion

    #region Red Pointer Methods
    public static int SetRedPointerMode(RedPointerMode mode)
    {
        // Returns void
        cci.ScSetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeRedpointerMode, (int)mode);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return 1;
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
    #endregion

    #region Mark Methods
    public (int status, double markTIme) GetLastMark()
    {
        var msg = "Get last mark requested...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        var markTime = 0.0; // placeholder for mark time

        if (cci.ScIsRunning() == 0)
        {
            msg = "Waverunner is not running.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, markTime);
        }

        try
        {
            markTime = cci.ScGetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeLastMarkTime);
            var mark_time_string = string.Concat("Last mark time was: ", markTime, " seconds");
            MagnetoLogger.Log(mark_time_string, LogFactoryLogLevel.LogLevel.VERBOSE);
            return (1, markTime);
        }
        catch (Exception exception)
        {
            msg = $"Unable to get mark time \n {Convert.ToString(exception)}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, markTime);
        }
    }
    public async Task<int> MarkEntityAsync(string filePath)
    {
        // File exists, proceed with marking
        var msg = $"Starting mark for file: {filePath}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        if (cci.ScIsRunning() == 0)
        {
            return 0;
        }
        // load demo job file
        cci.ScLoadJob(filePath, 1, 1, 0);
        msg = $"Loaded file at path: {filePath} for marking...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
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
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        msg = "SAMLight is stopping mark";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
        cci.ScStopMarking();
        return 1;
    }
    #endregion

    #region Slicing Methods
    // 	Check if SAMLight is running in SAM3D mode
    public bool InSAM3DMode()
    {
        // get current mode (0 = 2D, 1 = 3D)
        var mode = cci.ScGetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueType3D); // returns an integer
        return mode == 1;
    }

    public void ImportStlFile(string entityName, string filePath)
    {
        // NOTE: When using this command in SAM3D, 
        // last two arguments (resolution and flags) must be 0
        cci.ScImport(entityName, filePath, "stl", 0.0, 0); // returns void

        // TODO: check cci/waverunner for errors importing stl
    }

    public string? GenerateSlicedEntity(string inputEntityName, double sliceThickness)
    {
        // generate entity name
        var slicedEntityName = $"{inputEntityName}_Sliced";

        cci.ScSlice(
            inputEntityName,
            slicedEntityName,
            sliceThickness,
            0, // Slice all matching entities
            0  // Bottom-to-top (additive)
        ); // returns void

        // TODO: check cci/waverunner for errors generating slice

        //TODO: magneto log ($"Slicing successful: {slicedEntityName}");
        return slicedEntityName;
    }

    // export one slice
    public int ExportOneSliceToDirectory(string slicedEntityName, string outputDirectory, int sliceToExport) // TODO: update to export a specific slice
    {
        Directory.CreateDirectory(outputDirectory);

        // TODO: add guards to make sure user isn't accessing slice that doesn't exist
        var totalSlices = cci.ScGetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeGetTotalSlices);

        if (sliceToExport < 0 || sliceToExport > totalSlices)
        {
            MagnetoLogger.Log("requested export slice is out of index", LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        // WARNING: not sure if slices start on 0 or 1
        cci.ScSetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeCurrentSliceNum, sliceToExport);
    
        var filename = Path.Combine(outputDirectory, $"slice_{0:D4}.plt"); // TODO: update saved name when you update to single slice too

        var flags = 0x10 | 0x100; // export poly lines and pen settings

        cci.ScExport(slicedEntityName, filename, "plt", 0.001, flags); // returns void
        return 1;
    }

    // export all slices
    public void ExportAndSaveSlices(string slicedEntityName, string outputDirectory)
    {
        // get total slices so we can iterate through all of them and save later
        var totalSlices = cci.ScGetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeGetTotalSlices);

        if (totalSlices <= 0)
        {
            //TODO: magneto log ("No slices available for export.");
            return;
        }

        Directory.CreateDirectory(outputDirectory);

        for (var i = 1; i <= totalSlices; i++)
        {
            cci.ScSetLongValue(
                (int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeCurrentSliceNum, i);

            var filename = Path.Combine(outputDirectory, $"slice_{i:D4}.plt");
            int flags = 0x10;
            //flags |= 0x100;

            MagnetoLogger.Log(
                $"Exporting slice {i}/{totalSlices} of {slicedEntityName} -> {filename}",
                LogFactoryLogLevel.LogLevel.VERBOSE);

            try
            {
                cci.ScExport(slicedEntityName, filename, "plt", 1.0, flags);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // If slices are still saved, just log and ignore
                MagnetoLogger.Log(
                    $"Non-fatal COMException exporting slice {i}: {ex.Message}",
                    LogFactoryLogLevel.LogLevel.VERBOSE);
            }

            // does not work; first arg is suppose to be the entity name you want to use?
            //var entityName = $"slice_{i:D4}";
            //cci.ScImport(entityName, filename, "plt", 0.0, 0); // returns void
        }

        //TODO: magneto log ($"Export complete: {totalSlices} slices saved to {outputDirectory}");
    }
    #endregion
}

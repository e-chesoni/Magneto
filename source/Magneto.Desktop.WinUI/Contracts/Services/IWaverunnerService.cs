using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Magneto.Desktop.WinUI.Services.WaverunnerService;

namespace Magneto.Desktop.WinUI.Contracts.Services;
public interface IWaverunnerService
{
    #region Connection Check Methods
    bool IsRunning();
    int TestConnection();
    #endregion

    #region Helpers
    double CalculateEnergyDensity(double layerThickness, double power, double scanSpeed, double hatchSpacing);
    #endregion
    
    #region Get Pen/Mark Settings
    int GetPenNumber(string entityName);
    double GetMarkSpeed();
    double GetLaserPower();
    double GetDefaultMarkSpeed();
    double GetDefaultLaserPower();
    double GetDefaultSupplyAmplifier();
    #endregion
    
    #region Set Mark Speed/Laser Power
    void SetMarkSpeed(double markSpeed);
    void SetLaserPower(double power);
    #endregion

    #region Red Pointer Methods
    public int SetRedPointerMode(RedPointerMode mode);
    int StartRedPointer(string filePath);
    int StopRedPointer();
    #endregion

    #region Marking Methods
    (int status, double markTIme) GetLastMark();
    Task<int> MarkEntityAsync(string filePath);
    int GetMarkStatus();
    int StopMark();
    #endregion

    #region Hatching Methods
    double GetDefaultHatchSpacing();
    public void SetHatchDistance(string entityName, double hatchDistance);
    public void EnableAndSetHatchStyle(string entityName);
    #endregion
    
    #region 3D Slicing Methods
    bool InSAM3DMode();
    void ImportStlFile(string entityName, string filePath);
    string? GenerateSlicedEntity(string inputEntityName, double sliceThickness);
    void ExportAndSaveSlices(string slicedEntityName, string outputDirectory);
    #endregion
}

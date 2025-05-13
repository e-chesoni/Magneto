using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Contracts.Services;
public interface IWaverunnerService
{
    bool IsRunning();
    int TestConnection();
    int GetPenNumber(string entityName);
    double GetMarkSpeed();
    double GetLaserPower();
    double GetDefaultMarkSpeed();
    double GetDefaultLaserPower();
    double GetDefaultHatchSpacing();
    double GetDefaultSupplyAmplifier();
    double CalculateEnergyDensity(double layerThickness, double power, double scanSpeed, double hatchSpacing);
    void SetMarkSpeed(double markSpeed);
    void SetLaserPower(double power);
    int StartRedPointer(string filePath);
    int StopRedPointer();
    (int status, double markTIme) GetLastMark();
    Task<int> MarkEntityAsync(string filePath);
    int GetMarkStatus();
    int StopMark();
}

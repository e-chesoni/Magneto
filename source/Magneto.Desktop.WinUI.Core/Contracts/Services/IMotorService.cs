using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;
public interface IMotorService
{
    void Initialize();
    void HandleStartUp();
    public ActuationManager GetActuationManager();
    StepperMotor GetBuildMotor();
    StepperMotor GetPowderMotor();
    StepperMotor GetSweepMotor();
    public Task<double> GetMotorPosition(StepperMotor motor);
    void ConfigurePortEventHandlers();
    void InitMotors();
    void HandleMotorInit(string motorName, StepperMotor motor, out StepperMotor motorField);
    void InitializeMotorMap();
    Task<int> MoveMotorAbs(StepperMotor motor, double target);
    Task<int> MoveMotorRel(StepperMotor motor, double distance);
    Task<int> LayerMove(double layerThickness, double supplyAmplifier);
    bool MotorsRunning();
    Task<int> StopMotor(StepperMotor motor);
    Task<int> HomeMotor(StepperMotor motor);
}

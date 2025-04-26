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
    public CommandQueueManager GetActuationManager();
    StepperMotor GetBuildMotor();
    StepperMotor GetPowderMotor();
    StepperMotor GetSweepMotor();
    double GetMaxSweepPosition();
    public Task<double> GetMotorPositionAsync(StepperMotor motor);
    public Task<double> GetMotorPositionAsync(string motorName);
    Task<double> GetBuildMotorPositionAsync();
    Task<double> GetPowderMotorPositionAsync();
    Task<double> GetSweepMotorPositionAsync();
    void ConfigurePortEventHandlers();
    void IntializeMotors();
    void HandleMotorInit(string motorName, StepperMotor motor, out StepperMotor motorField);
    void InitializeMotorMap();
    Task<int> MoveBuildMotor(bool moveAbs, double value);
    Task<int> MovePowderMotor(bool moveAbs, double value);
    Task<int> MoveSweepMotor(bool moveAbs, double value);
    Task<int> MoveMotorAbs(string motorName, double target);
    Task<int> MoveBuildMotorAbs(double target);
    Task<int> MovePowderMotorAbs(double target);
    Task<int> MoveSweepMotorAbs(double target);
    Task<int> MoveMotorRel(StepperMotor motor, double distance);
    Task<int> MoveMotorRel(string motorName, double distance);
    Task<int> LayerMove(double layerThickness, double supplyAmplifier);
    bool MotorsRunning();
    bool CheckMotorStopFlag(string motorName);
    Task<int> StopMotorAndClearQueue(StepperMotor motor);
    Task<int> HomeMotor(StepperMotor motor);
    Task<int> HomeMotor(string motorName);
    Task<int> WaitUntilMotorHomedAsync(string motorName);
    //Task<int> WaitUntilAtTargetAsync(StepperMotor motor, double targetPos);
    Task<int> WaitUntilBuildReachesTargetAsync(double targetPos);
    Task<int> WaitUntilPowderReachesTargetAsync(double targetPos);
    Task<int> WaitUntilSweepReachesTargetAsync(double targetPos);
}

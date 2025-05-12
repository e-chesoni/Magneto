using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;
public interface IMotorService
{
    #region Start Up
    void ConfigurePortEventHandlers();
    void IntializeMotors();
    void HandleMotorInit(string motorNameLowerCase, StepperMotor motor, out StepperMotor motorField);
    void InitializeMotorMap();
    void HandleStartUp();
    #endregion

    #region Getters
    public StepperMotor GetBuildMotor();
    public StepperMotor GetPowderMotor();
    public StepperMotor GetSweepMotor();
    
    #region Position Getters
    Task<double> GetBuildMotorPositionAsync();
    Task<double> GetPowderMotorPositionAsync();
    Task<double> GetSweepMotorPositionAsync();
    Task<(int res, double position)> GetMotorPositionAsync(string motorNameLowerCase);
    #endregion
    #endregion

    #region Read and Clear Motor Errors
    public Task ReadBuildMotorErrors();
    public Task ReadPowderMotorErrors();
    public Task ReadSweepMotorErrors();
    public Task ReadAndClearAllErrors();
    #endregion

    public void AddProgramFront(string motorNameLower, string[] program);

    #region Send Program
    public void SendProgram(string motorNameLower, string[] program);
    #endregion

    #region Is Program Running
    public Task<bool> IsProgramRunningAsync(string motorNameLower);
    #endregion

    #region Pause, Resume, Clear Program
    //public bool IsProgramPaused();
    public void EnableProgram();
    public void PauseProgram();
    //public Task ResumeProgramReading();
    public Task ResumeProgram();
    public void ClearProgramList();
    #endregion

    #region Stop Motors
    public void StopMotorAndClearProgramList(string motorNameLower);
    public void StopAllMotorsClearProgramList();
    //public void EmergencyStop();
    //public bool IsProgramStopped();
    #endregion

    public Task MoveMotorAbsoluteProgram(string motorNameLower, double distance);
    public Task MoveMotorRelativeProgram(string motorNameLower, double distance, bool moveUp);
    Task<int> HomeMotorProgram(string motorNameLowerCase);
    public Task HomeAllMotors();
}

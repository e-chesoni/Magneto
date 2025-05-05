using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.ProgramsManager;

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
    public int GetMotorAxis(string motorName);
    
    #region Position Getters
    double GetMaxSweepPosition();
    Task<double> GetBuildMotorPositionAsync();
    Task<double> GetPowderMotorPositionAsync();
    Task<double> GetSweepMotorPositionAsync();
    Task<(int res, double position)> GetMotorPositionAsync(string motorNameLowerCase);
    #endregion

    #region Program Getters
    public int GetNumberOfPrograms();
    public ProgramNode? GetFirstProgramNode();
    public ProgramNode? GetLastProgramNode();
    #endregion
    #endregion

    #region Read and Clear Motor Errors
    public Task ReadBuildMotorErrors();
    public Task ReadPowderMotorErrors();
    public Task ReadSweepMotorErrors();
    public Task ReadAndClearAllErrors();
    #endregion

    #region Write Program
    public string[] WriteAbsoluteMoveProgramForBuildMotor(double target);
    public string[] WriteAbsoluteMoveProgramForPowderMotor(double target);
    public string[] WriteAbsoluteMoveProgramForSweepMotor(double target);
    public string[] WriteRelativeMoveProgramForBuildMotor(double steps, bool moveUp);
    public string[] WriteRelativeMoveProgramForPowderMotor(double steps, bool moveUp);
    public string[] WriteRelativeMoveProgramForSweepMotor(double steps, bool moveUp);
    #endregion

    #region Send Program
    public void SendProgram(string motorNameLower, string[] program);
    #endregion

    #region Is Program Running
    public Task<bool> IsProgramRunningAsync(string motorNameLower);
    #endregion

    #region Add Program Front
    public void AddProgramFront(string motorNameLower, string[] program);
    public void AddProgramLast(string motorNameLower, string[] program);
    #endregion

    #region Pause and Resume Program
    public bool IsProgramPaused();
    public void PauseProgram();
    public Task ResumeProgramReading();
    #endregion

    #region Stop Motors
    public void StopMotorAndClearProgramList(string motorNameLower);
    public void StopAllMotorsClearProgramList();
    public void EmergencyStop();
    #endregion

    #region Multi-Motor Move Methods
    public (string[] program, Controller controller, int axis)? ExtractProgramNodeVariables(ProgramNode programNode);
    public Task ExecuteLayerMove(double thickness, double amplifier);
    #endregion

    public Task MoveMotorAbsoluteProgram(string motorNameLower, double distance);
    public Task MoveMotorRelativeProgram(string motorNameLower, double distance, bool moveUp);
    Task<int> HomeMotorProgram(string motorNameLowerCase);
    public Task HomeAllMotors();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.CommandQueueManager;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;
public interface IMotorService
{
    #region Start Up
    void ConfigurePortEventHandlers();
    void IntializeMotors();
    void HandleMotorInit(string motorNameLowerCase, StepperMotor motor, out StepperMotor motorField);
    void InitializeMotorMap();
    void HandleStartUp();
    //void Initialize();
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
    public string[] WriteAbsoluteMoveProgramForBuildMotor(double target, bool moveUp);
    public string[] WriteAbsoluteMoveProgramForPowderMotor(double target, bool moveUp);
    public string[] WriteAbsoluteMoveProgramForSweepMotor(double target, bool moveUp);
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
    public void StopMotor(string motorNameLower);
    public void StopAllMotors();
    #endregion

    #region Multi-Motor Move Methods
    public (string[] program, Controller controller, int axis)? ExtractProgramNodeVariables(ProgramNode programNode);
    public Task ExecuteLayerMove(double thickness, double amplifier, int numberOfLayers);
    #endregion





    Task<int> MoveMotorAbs(string motorNameLowerCase, double target);
    Task<int> MoveBuildMotorAbs(double target);
    Task<int> MovePowderMotorAbs(double target);
    Task<int> MoveSweepMotorAbs(double target);
    Task<int> MoveMotorRel(string motorNameLowerCase, double distance);
    Task<int> MoveBuildMotorRel(double distance);
    Task<int> MovePowderMotorRel(double distance);
    Task<int> MoveSweepMotorRel(double distance);

    public Task MoveMotorRelativeProgram(string motorNameLower, double distance, bool moveUp);

    bool MotorsRunning();
    bool CheckMotorStopFlag(string motorNameLowerCase);
    void EnableBuildMotor();
    void EnablePowderMotor();
    void EnableSweepMotor();
    void EnableMotors();
    Task<int> StopMotorAndClearQueue(string motorNameLowerCase);
    Task<int> HomeMotor(StepperMotor motor);
    Task<int> HomeMotor(string motorNameLowerCase);
    Task<int> WaitUntilMotorHomedAsync(string motorNameLowerCase);
    Task<int> WaitUntilBuildReachesTargetAsync(double targetPos);
    Task<int> WaitUntilPowderReachesTargetAsync(double targetPos);
    Task<int> WaitUntilSweepReachesTargetAsync(double targetPos);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Motor.StepperMotor;

namespace Magneto.Desktop.WinUI.Core.Models;
public class MissionControl : IMediator, IPublisher, ISubsciber
{
    #region Private Variables
    
    /// <summary>
    /// Build manager to handle printing tasks
    /// </summary>
    private ActuationManager _actuationManager { get; set; }

    /// <summary>
    /// A list of subscribers that want to receive notifications from Mission Control
    /// </summary>
    private List<ISubsciber> _subscibers;

    #endregion


    #region Public Variables

    /// <summary>
    /// Message for testing
    /// TODO: Remove later; testing that we can reach mission control from all pages
    /// </summary>
    public string FriendlyMessage = "Hello from Mission Control!";

    #endregion


    #region Constructor

    /// <summary>
    /// Mission control constructor
    /// </summary>
    /// <param name="bm"></param> Build manager
    public MissionControl(ActuationManager _am)
    {
        MagnetoLogger.Log("", LogFactoryLogLevel.LogLevel.VERBOSE);

        _actuationManager = _am;

    }

    #endregion


    #region Initialization Methods

    /// <summary>
    /// Generate an artifact model from a path to an artifact
    /// </summary>
    /// <param name="path_to_artifact"> File path to artifact</param>
    public void CreateArtifactModel(string path_to_artifact)
    {
        _actuationManager.artifactModel = new ArtifactModel(path_to_artifact);
    }

    #endregion


    #region Getters

    public ActuationManager GetActuationManger()
    {
        return _actuationManager;
    }

    /// <summary>
    /// Get the total height of the current print
    /// </summary>
    /// <returns></returns>
    public double GetCurrentPrintHeight()
    {
        return _actuationManager.GetCurrentPrintHeight();
    }

    /// <summary>
    /// Get all motors define in build manager
    /// </summary>
    /// <returns>List of motors</returns>
    public List<StepperMotor> GetMotorList()
    {
        return _actuationManager.GetMotorList();
    }

    /// <summary>
    /// Get powder motor from build manager
    /// </summary>
    /// <returns></returns>
    public StepperMotor GetPowderMotor()
    {
        return _actuationManager.GetPowderMotor();
    }

    /// <summary>
    /// Get build motor from build manager
    /// </summary>
    /// <returns></returns>
    public StepperMotor GetBuildMotor()
    {
        return _actuationManager.GetBuildMotor();
    }

    /// <summary>
    /// Get sweep motor from build manager
    /// </summary>
    /// <returns></returns>
    public StepperMotor GetSweepMotor()
    {
        return _actuationManager.GetSweepMotor();
    }

    /// <summary>
    /// Get the layer thickness for the print from the build manager
    /// </summary>
    /// <returns> double artifact layer thickness </returns>
    public double GetDefaultArtifactThickness()
    {
        return _actuationManager.GetDefaultArtifactThickness();
    }


    #endregion


    #region Setters

    /// <summary>
    /// Set the layer thickness for the print on the build manager
    /// </summary>
    /// <param name="thickness"> Desired thickness </param>
    public void SetArtifactThickness(double thickness)
    {
        _actuationManager.SetArtifactThickness(thickness);
    }

    #endregion


    #region Operations Delegated to BuildManager

    /// <summary>
    /// Slice an artifact using build manager (resulting in a dance that gest stored on the build manager)
    /// </summary>
    public void SliceArtifact()
    {
        // TODO: ARTIFACT HANDLER CONTROLS SLICE NUMBER (SliceArtifact calls ArtifactHandler method)
        _actuationManager.SliceArtifact();
    }

    /// <summary>
    /// Use the build manager to start a print
    /// </summary>
    public void StartPrint()
    {
        if (_actuationManager.artifactModel.sliceStack.Count == 0) // TODO: FIX bm.artifactModel is null error
        {
            var msg = "There are no slices on this artifact model. Are you sure you sliced it?";
            MagnetoLogger.Log(msg,
            LogFactoryLogLevel.LogLevel.ERROR);
        }
        else
        {
            // Call build manager to handle print
            _actuationManager.StartPrint(_actuationManager.artifactModel);
        }
    }

    /// <summary>
    /// Pause a print that the build manager is handling
    /// </summary>
    public void PausePrint()
    {
        _actuationManager.Pause();
    }

    /// <summary>
    /// Resume a print using the build manager
    /// </summary>
    public void Resume()
    {
        _actuationManager.Resume();
    }

    /// <summary>
    /// Cancel the print the build manager is currently executing
    /// </summary>
    public void CancelPrint()
    {
        _actuationManager.Cancel();
    }

    /// <summary>
    /// Home motors using the build manager
    /// </summary>
    public void HomeMotors()
    {
        _actuationManager.HomeMotors();
    }

    #endregion


    #region Mediator Methods
    public int Mediate(object sender, string ev) => throw new NotImplementedException();

    #endregion


    #region Publisher Methods

    public int Attach(ISubsciber subscriber)
    {
        _subscibers.Add(subscriber);
        return 0;
    }

    public int Detach(ISubsciber subscriber)
    {
        _subscibers.Remove(subscriber);
        return 0;
    }
    public int Notify(ISubsciber subsciber)
    {
        subsciber.HandleUpdate(this);
        return 0;
    }

    public int NotifyAll()
    {
        foreach (var s in _subscibers) { s.HandleUpdate(this); }
        return 0;
    }

    #endregion


    #region Subscriber Methods

    public void ReceiveUpdate(IPublisher publisher) => throw new NotImplementedException();
    public void HandleUpdate(IPublisher publisher) => throw new NotImplementedException();

    #endregion
}

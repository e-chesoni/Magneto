using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Image;
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
    private BuildManager _buildManager { get; set; }

    //private List<StepperMotor> _motorList { get; set; } = new List<StepperMotor>();

    /// <summary>
    /// A list of subscribers that want to receive notifications from Mission Control
    /// </summary>
    private List<ISubsciber> _subscibers;

    /// <summary>
    /// Amount motor steps when bed level button is pushed up or down
    /// Value is in mm
    /// </summary>
    private double _bedLevelStep { get; set; }

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
    public MissionControl(BuildManager bm)
    {
        MagnetoLogger.Log("", LogFactoryLogLevel.LogLevel.VERBOSE);

        _buildManager = bm;
        _bedLevelStep = 1; // mm
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Generate an image model from a path to an image
    /// </summary>
    /// <param name="path_to_image"> File path to image </param>
    public void CreateImageModel(string path_to_image)
    {
        _buildManager.imageModel = new ImageModel();
        _buildManager.SetImagePath(path_to_image);
    }

    #endregion

    #region Getters

    public double GetCurrentBuildHeight()
    {
        return _buildManager.GetCurrentPrintHeight();
    }

    /// <summary>
    /// Get all motors define in build manager
    /// </summary>
    /// <returns>List of motors</returns>
    public List<StepperMotor> GetMotorList()
    {
        return _buildManager.GetMotorList();
    }

    /// <summary>
    /// Get powder motor from build manager
    /// </summary>
    /// <returns></returns>
    public StepperMotor GetPowderMotor()
    {
        return _buildManager.GetPowderMotor();
    }

    /// <summary>
    /// Get build motor from build manager
    /// </summary>
    /// <returns></returns>
    public StepperMotor GetBuildMotor()
    {
        return _buildManager.GetBuildMotor();
    }

    /// <summary>
    /// Get sweep motor from build manager
    /// </summary>
    /// <returns></returns>
    public StepperMotor GetSweepMotor()
    {
        return _buildManager.GetSweepMotor();
    }

    /// <summary>
    /// Get the layer thickness for the print from the build manager
    /// </summary>
    /// <returns> double image layer thickness </returns>
    public double GetImageThickness()
    {
        return _buildManager.GetImageThickness();
    }

    /// <summary>
    /// Get step distance for bed leveling
    /// </summary>
    /// <returns></returns>
    public double GedBedLevelStep()
    {
        return _bedLevelStep;
    }

    public double GetDefaultPrintLayerThickness()
    {
        return MagnetoConfig.GetDefaultPrintThickness();
    }


    #endregion

    #region Setters

    /// <summary>
    /// Set the layer thickness for the print on the build manager
    /// </summary>
    /// <param name="thickness"> Desired thickness </param>
    public void SetImageThickness(double thickness)
    {
        _buildManager.SetImageThickness(thickness);
    }

    /// <summary>
    /// Set bed leveling step
    /// </summary>
    /// <param name="step"></param>
    public void SetBedLevelStep(double step)
    {
        _bedLevelStep = step;
    }

    #endregion

    #region Bed Leveling

    /// <summary>
    /// Helper for bed leveling
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="dir"></param>
    private void LevelHelper(int axis, MotorDirection dir)
    {
        if (dir == MotorDirection.Up)
        {
            _ = _buildManager.buildController.MoveMotorRel(axis, _bedLevelStep);
        }
        else
        {
            var step = -_bedLevelStep;
            _ = _buildManager.buildController.MoveMotorRel(axis, step);
        }
    }

    /// <summary>
    /// Moves motor on given axis in one bed level step in given direction
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="dir"></param>
    public void LevelMotor(int axis, MotorDirection dir)
    {
        if (axis > 0 && axis < 3)
        {
            LevelHelper(axis, dir);
        }
        else
        {
            MagnetoLogger.Log("Received invalid axis.",
                    LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    #region Operations Delegated to ImageManager

    /// <summary>
    /// Slice an image on the build manager
    /// </summary>
    public void SliceImage()
    {
        _buildManager.SliceImage();
    }

    #endregion

    #region Operations Delegated to BuildManager

    /// <summary>
    /// Use the build manager to start a print
    /// </summary>
    public void StartPrint()
    {
        if (_buildManager.imageModel.sliceStack.Count == 0) // TODO: FIX bm.imageModel is null error
        {
            MagnetoLogger.Log("There are no slices on this image model. Are you sure you sliced it?",
            LogFactoryLogLevel.LogLevel.ERROR);
        }
        else
        {
            // Call build manager to handle print
            _buildManager.StartPrint(_buildManager.imageModel);
        }
    }

    /// <summary>
    /// Pause a print that the build manager is handling
    /// </summary>
    public void PausePrint()
    {
        _buildManager.Pause();
    }

    /// <summary>
    /// Resume a print using the build manager
    /// </summary>
    public void Resume()
    {
        _buildManager.Resume();
    }

    /// <summary>
    /// Cancel the print the build manager is currently executing
    /// </summary>
    public void CancelPrint()
    {
        _buildManager.Cancel();
    }

    /// <summary>
    /// Home motors using the build manager
    /// </summary>
    public void HomeMotors()
    {
        _buildManager.HomeMotors();
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

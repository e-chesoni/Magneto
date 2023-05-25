using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models;
public class MissionControl : IMediator, IPublisher, ISubsciber
{
    private BuildManager _buildManager { get; set; }

    private List<ISubsciber> _subscibers;

    // TODO: Remove later; testing that we can reach mission control from all pages
    public string FriendlyMessage = "Hello from Mission Control!";

    #region Constructor

    public MissionControl(BuildManager bm)
    {
        MagnetoLogger.Log("MissionControl::MissionControl",
            LogFactoryLogLevel.LogLevel.VERBOSE);

        _buildManager = bm;
    }

    #endregion

    #region Operations Delegated to ImageManager

    public void SliceImage(ImageModel im)
    {
        im.sliceStack = ImageHandler.SliceImage(im);
    }

    #endregion

    #region Operations Delegated to BuildManager

    public void StartPrint(ImageModel im)
    {
        if (im.sliceStack.Count == 0)
        {
            MagnetoLogger.Log("BuildManager::StartPrint -- There are no slices on this image model. Are you sure you sliced it?",
            LogFactoryLogLevel.LogLevel.ERROR);
        }
        else
        {
            // Slice image
            SliceImage(im);

            // Pass image model with sliced image to build manager to handle print
            _buildManager.StartPrint(im);
        }
    }

    public void PausePrint()
    {
        _buildManager.Pause();
    }

    public void Resume()
    {
        _buildManager.Resume();
    }

    public void CancelPrint()
    {
        _buildManager.Cancel();
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

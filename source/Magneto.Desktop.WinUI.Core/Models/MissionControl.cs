using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using static Magneto.Desktop.WinUI.Core.Models.Motors.StepperMotor;
using Magneto.Desktop.WinUI.Core.Contracts;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;

namespace Magneto.Desktop.WinUI.Core.Models;
public class MissionControl : IMediator, IPublisher, ISubsciber
{
    // TODO: you need to rethink how this is set up (see test print page and work from there)
    #region Private Variables
    private PrintStateMachine _printStateMachine;
    private List<ISubsciber> _subscibers;
    #endregion

    #region Constructor
    /// <summary>
    /// Mission control constructor
    /// </summary>
    /// <param name="bm"></param> Build manager
    public MissionControl(PrintStateMachine psm)
    {
        _printStateMachine = psm;
        MagnetoLogger.Log("Hello from Mission Control!", LogFactoryLogLevel.LogLevel.VERBOSE);
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

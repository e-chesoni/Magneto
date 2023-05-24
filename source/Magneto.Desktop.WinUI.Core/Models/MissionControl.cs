using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Models;
public class MissionControl : IMediator, IPublisher, ISubsciber
{
    private MotorController _motorController;

    #region Mediator Methods
    public int Mediate(object sender, string ev) => throw new NotImplementedException();

    #endregion

    #region Publisher Methods

    public int Attach() => throw new NotImplementedException();
    public int Detach() => throw new NotImplementedException();
    public int Notify(ISubsciber subsciber) => throw new NotImplementedException();
    public int NotifyAll() => throw new NotImplementedException();

    #endregion

    #region Subscriber Methods

    public void ReceiveUpdate(IPublisher publisher) => throw new NotImplementedException();
    public void HandleUpdate(IPublisher publisher) => throw new NotImplementedException();

    #endregion
}

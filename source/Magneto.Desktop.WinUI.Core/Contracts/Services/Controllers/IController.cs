using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
public interface IController
{
    // Default motor list so build manager queue processing can work
    List<StepperMotor> GetMinions() => null; // dumb, but has to be here because of queuing in actuation manager
}

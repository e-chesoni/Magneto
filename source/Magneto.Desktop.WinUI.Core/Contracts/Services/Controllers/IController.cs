using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motor;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
public interface IController
{
    // Default motor list so build manager queue processing can work
    List<StepperMotor> GetMotorList() => null;
}

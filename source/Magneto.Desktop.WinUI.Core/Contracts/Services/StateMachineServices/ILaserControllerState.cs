using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
public interface ILaserControllerState
{
    /// <summary>
    /// Method to handle Draw command in current state 
    /// </summary>
    void Draw();

    /// <summary>
    /// Method to handle Pause command in current state 
    /// </summary>
    void Pause();

    /// <summary>
    /// Method to handle Cancel command in current state 
    /// </summary>
    void Cancel();
}

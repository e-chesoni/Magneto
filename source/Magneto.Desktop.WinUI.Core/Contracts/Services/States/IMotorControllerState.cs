using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.States;

/// <summary>
/// Interface for Magneto Motor Controller States
/// </summary>
public interface IMotorControllerState
{
    /// <summary>
    /// Method to handle a Move command in current state
    /// </summary>
    void Move();

    /// <summary>
    /// Method to handle a Wait command in current state
    /// </summary>
    void Wait();

    /// <summary>
    /// Method to handle a Done command in current state
    /// </summary>
    void Done();

    /// <summary>
    /// Method to handle a Cancel command in current state
    /// </summary>
    void Cancel();

}

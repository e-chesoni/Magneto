using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
public interface IStateMachine
{
    /// NOTE: All classes should implement a TransistionTo(State s) method
    /// That accepts their respective state

    /// <summary>
    ///  Cancel state machine process that is currently running 
    /// </summary>
    void Cancel();
}

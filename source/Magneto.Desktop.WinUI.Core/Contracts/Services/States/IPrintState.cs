using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.States;

/// <summary>
/// Interface for Magneto Build Manager States
/// </summary>
public interface IPrintState
{
    void Play();
    void Pause();
    void Redo();
    void Cancel();
    void ChangeStateTo(IPrintState state);
}

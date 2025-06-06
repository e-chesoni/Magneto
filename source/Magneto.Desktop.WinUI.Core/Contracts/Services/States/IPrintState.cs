﻿using System;
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
    Task<bool> InitializePlayAsync(int numberOfLayers = 1);
    Task<bool> Play(int numberOfLayers = 1);
    void Pause();
    Task<bool> Resume();
    void Redo();
    void Cancel();
    void ChangeStateTo(IPrintState state);
}

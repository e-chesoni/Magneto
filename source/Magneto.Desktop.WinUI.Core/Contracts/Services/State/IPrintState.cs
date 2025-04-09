using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Artifact;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.State;

/// <summary>
/// Interface for Magneto Build Manager States
/// </summary>
public interface IBuildManagerState
{
    void Start(ArtifactModel im);

    /// <summary>
    /// Method to handle a Draw command in current state
    /// </summary>
    Task Draw();

    /// <summary>
    /// Method to handle a Pause command in current state
    /// </summary>
    void Pause();

    /// <summary>
    /// Method to handle a Resume command in current state
    /// </summary>
    void Resume();

    /// <summary>
    /// Method to handle a Done command in current state
    /// </summary>
    void Done();

    /// <summary>
    /// Method to handle a Cancel command in current state
    /// </summary>
    void Cancel();

    /// <summary>
    /// Method to handle a Homing command in current state
    /// </summary>
    Task Homing();
}

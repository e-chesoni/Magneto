using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;

namespace Magneto.Desktop.WinUI.Core.Models.Controllers;

/// <summary>
/// Class used to control laser and scan head
/// </summary>
public class LaserController : IController
{
    /// <summary>
    /// LaserController constructor
    /// </summary>
    public LaserController()
    {

    }

    internal void Draw(Slice slice)
    {
        MagnetoLogger.Log("Placeholder for laser to draw current slice...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
    }

}

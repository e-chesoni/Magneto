using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;

/// <summary>
/// Interface for class that coordinates tasks across two or more classes
/// </summary>
public interface IMediator
{
    int Mediate(object sender, string ev);
}

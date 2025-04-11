using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts;

/// <summary>
/// Interface for class that subscribes to a publisher
/// </summary>
public interface ISubsciber
{
    // Receive update from publisher
    void ReceiveUpdate(IPublisher publisher);

    // Handle update from publisher
    void HandleUpdate(IPublisher publisher);
}

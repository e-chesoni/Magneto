using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;

/// <summary>
/// Interface for class that publishes messages to a list of subscribers
/// </summary>
public interface IPublisher
{
    // Attach subscriber to publisher
    int Attach();

    // Detach subscriber from publisher
    int Detach();

    // Notify all subscribers
    int NotifyAll();

    // Notify specific subscriber
    int Notify(ISubsciber subsciber);
}

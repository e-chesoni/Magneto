using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts;

/// <summary>
/// Interface for class that publishes messages to a list of subscribers
/// </summary>
public interface IPublisher
{
    // Attach subscriber to publisher
    int Attach(ISubsciber subscriber);

    // Detach subscriber from publisher
    int Detach(ISubsciber subscriber);

    // Notify all subscribers
    int NotifyAll();

    // Notify specific subscriber
    int Notify(ISubsciber subsciber);
}

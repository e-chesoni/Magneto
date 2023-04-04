using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagnetoLibrary.Interfaces
{
    /// <summary>
    /// 
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
}

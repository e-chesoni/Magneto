using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MagnetoLibrary.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISubsciber
    {
        // Receive update from publisher
        void ReceiveUpdate(IPublisher publisher);

        // Handle update from publisher
        void HandleUpdate(IPublisher publisher);
    }
}

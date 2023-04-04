using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MagnetoLibrary.Interfaces;
using MagnetoLibrary.Motor;

namespace MagnetoLibrary
{
    public class MissionControl : IMediator, IPublisher, ISubsciber
    {
        private StepperMotorController _sController;

        // Mediator Methods
        public int Attach()
        {
            throw new NotImplementedException();
        }

        // Publisher Methods
        public int Detach()
        {
            throw new NotImplementedException();
        }

        public void HandleUpdate(IPublisher publisher)
        {
            throw new NotImplementedException();
        }

        public int Notify(object sender, string ev)
        {
            throw new NotImplementedException();
        }

        public int Notify(ISubsciber subsciber)
        {
            throw new NotImplementedException();
        }

        // Subscriber Methods
        public int NotifyAll()
        {
            throw new NotImplementedException();
        }

        public void ReceiveUpdate(IPublisher publisher)
        {
            throw new NotImplementedException();
        }
    }
}

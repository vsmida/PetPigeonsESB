using System.Threading;

namespace ZmqServiceBus.Transport
{
    public interface IQosManager
    {
        void RegisterMessage(ITransportMessage transportMessage, IQosStrategy strategy);
        void InspectMessage(ITransportMessage transportMessage);
    }

    public class QosManager : IQosManager
    {
        public void RegisterMessage(ITransportMessage transportMessage, IQosStrategy strategy)
        {
            throw new System.NotImplementedException();
        }

        public void InspectMessage(ITransportMessage transportMessage)
        {
            throw new System.NotImplementedException();
        }
    }
}
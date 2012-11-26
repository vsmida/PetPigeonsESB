using System;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IPersistenceSynchronizer
    {
        event Action<string> MessageTypeSynchronizationRequested;
        event Action<string, string> MessageTypeForPeerSynchronizationRequested;
        void SynchronizeMessageType(string messageType);
        void SynchronizeMessageType(string messageType, string peer);
    }

   public class BrokerPersistenceSynchronizer : IPersistenceSynchronizer
    {
        public event Action<string> MessageTypeSynchronizationRequested = delegate{};
        public event Action<string, string> MessageTypeForPeerSynchronizationRequested = delegate{};
       private IMessageSender _messageSender;

       public BrokerPersistenceSynchronizer(IMessageSender messageSender)
       {
           _messageSender = messageSender;
       }

       public void SynchronizeMessageType(string messageType)
        {
            _messageSender.Send(new SynchronizeMessageCommand(messageType));
            MessageTypeSynchronizationRequested(messageType);
        }

        public void SynchronizeMessageType(string messageType, string peer)
        {
            _messageSender.Send(new SynchronizePeerMessageCommand(messageType, peer));
            MessageTypeForPeerSynchronizationRequested(messageType, peer);
        }
    }
}
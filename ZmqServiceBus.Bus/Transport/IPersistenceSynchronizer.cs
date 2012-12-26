using System;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IPersistenceSynchronizer
    {
        event Action<string> MessageTypeSynchronizationRequested;
        event Action<string, string> MessageTypeForPeerSynchronizationRequested;
        void SynchronizeMessageType(string messageType);
        void SynchronizeMessageType(string messageType, string peer);
        ICompletionCallback PersistMessage(ISendingBusMessage message);
    }

   public class BrokerPersistenceSynchronizer : IPersistenceSynchronizer
    {
       
        public event Action<string> MessageTypeSynchronizationRequested = delegate{};
        public event Action<string, string> MessageTypeForPeerSynchronizationRequested = delegate{};
       private readonly IMessageSender _messageSender;
       private readonly IMessageOptionsRepository _messageOptionsRepository;
       private readonly ISendingStrategyStateManager _sendingStrategyStateManager;

       public BrokerPersistenceSynchronizer(IMessageSender messageSender, IMessageOptionsRepository messageOptionsRepository)
       {
           _messageSender = messageSender;
           _messageOptionsRepository = messageOptionsRepository;
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

       public ICompletionCallback PersistMessage(ISendingBusMessage message)
       {
           return _messageSender.Route(new PersistMessageCommand(message),_messageOptionsRepository.GetOptionsFor(message.MessageType).ReliabilityInfo.BrokerName);   
       }
    }
}
using System;
using System.Collections.Concurrent;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public interface ISendingStrategyManager
    {
        ISendingReliabilityStrategy GetSendingStrategy(IReceivedTransportMessage message);
        void RegisterMessageId(Guid messageId, ISendingReliabilityStrategy strategy);
    }



    public class SendingStrategyManager : ISendingStrategyManager
    {
        private readonly ConcurrentDictionary<Guid, ISendingReliabilityStrategy> _messageIdToReliabilityInfo = new ConcurrentDictionary<Guid, ISendingReliabilityStrategy>();

        public SendingStrategyManager(IEndpointManager endpointManager)
        {
        }


        public ISendingReliabilityStrategy GetSendingStrategy(IReceivedTransportMessage message)
        {
            return null;
        }

        public void RegisterMessageId(Guid messageId, ISendingReliabilityStrategy strategy)
        {
            _messageIdToReliabilityInfo.AddOrUpdate(messageId, strategy, (key, oldValue) => strategy);
        }



    }

    public interface IReliabilityLayer : IDisposable
    {
        void RegisterMessageReliabilitySetting(Type messageType, MessageOptions level);
        void Send(IReceivedTransportMessage message);
        void Publish(IReceivedTransportMessage message);
        void Route(IReceivedTransportMessage message, string destinationPeer);
        event Action<IReceivedTransportMessage> OnMessageReceived;
        void Initialize();
    }
}
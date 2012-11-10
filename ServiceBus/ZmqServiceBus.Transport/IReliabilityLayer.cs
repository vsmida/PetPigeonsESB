using System;
using System.Collections.Concurrent;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Transport
{
    public interface ISendingStrategyManager
    {
        ISendingReliabilityStrategy GetSendingStrategy(ITransportMessage message);
        void RegisterMessageId(Guid messageId, ISendingReliabilityStrategy strategy);
    }



    public class SendingStrategyManager : ISendingStrategyManager
    {
        private readonly ConcurrentDictionary<Guid, ISendingReliabilityStrategy> _messageIdToReliabilityInfo = new ConcurrentDictionary<Guid, ISendingReliabilityStrategy>();

        public SendingStrategyManager(IEndpointManager endpointManager)
        {
        }


        public ISendingReliabilityStrategy GetSendingStrategy(ITransportMessage message)
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
        void Send(ITransportMessage message);
        void Publish(ITransportMessage message);
        void Route(ITransportMessage message);
        event Action<ITransportMessage> OnMessageReceived;
        void Initialize();
    }
}
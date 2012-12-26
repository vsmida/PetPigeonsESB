using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    public interface ISendingReliabilityStrategy
    {
        IEnumerable<ISendingBusMessage> Send(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions);
        IEnumerable<ISendingBusMessage> Publish(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions);
        event Action ReliabilityAchieved;
    }

    abstract class SendingReliabilityStrategy : ISendingReliabilityStrategy
    {
        public abstract IEnumerable<ISendingBusMessage> Send(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions);
        public abstract IEnumerable<ISendingBusMessage> Publish(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions);
        public abstract event Action ReliabilityAchieved;

        protected ISendingBusMessage GetTransportMessage(IMessage message, IEnumerable<IEndpoint> endpoints)
        {
            return new SendingBusMessage(message.GetType().FullName, Guid.NewGuid(), Serializer.Serialize(message), endpoints);
        }
    }
}
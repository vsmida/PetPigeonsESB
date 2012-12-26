using System;
using System.Collections.Generic;
using System.Linq;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class FireAndForget : SendingReliabilityStrategy
    {

        public override IEnumerable<ISendingBusMessage> Send(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            ReliabilityAchieved();
            return new[] {GetTransportMessage(message, concernedSubscriptions.Select(x => x.Endpoint))  };
        }

        public override IEnumerable<ISendingBusMessage> Publish(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            ReliabilityAchieved();
            return new[] { GetTransportMessage(message, concernedSubscriptions.Select(x => x.Endpoint)) };
        }

        public override event Action ReliabilityAchieved = delegate{};
    }
}
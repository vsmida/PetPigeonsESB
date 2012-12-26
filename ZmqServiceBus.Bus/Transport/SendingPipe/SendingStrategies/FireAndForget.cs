using System;
using System.Collections.Generic;
using System.Linq;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class FireAndForget : ISendingReliabilityStrategy
    {
        private readonly IDataSender _dataSender;

        public FireAndForget(IDataSender dataSender)
        {
            _dataSender = dataSender;
        }

        public void Send(ISendingBusMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            _dataSender.SendMessage(message, concernedSubscriptions.Select(x => x.Endpoint));
            ReliabilityAchieved();
        }

        public void Publish(ISendingBusMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            _dataSender.SendMessage(message, concernedSubscriptions.Select(x => x.Endpoint));
            ReliabilityAchieved();
        }

        public event Action ReliabilityAchieved = delegate{};
    }
}
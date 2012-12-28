using System;
using System.Collections.Generic;
using System.Linq;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class FireAndForget : SendingReliabilityStrategy
    {
        public override void SetupCommandReliabilitySafeguards(ISendingBusMessage message)
        {
            ReliabilityAchieved();
        }

        public override void SetupEventReliabilitySafeguards(ISendingBusMessage message)
        {
            ReliabilityAchieved();
        }

        public override event Action ReliabilityAchieved;
    }
}
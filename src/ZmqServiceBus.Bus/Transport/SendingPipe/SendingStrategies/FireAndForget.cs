using System;
using System.Collections.Generic;
using System.Linq;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class FireAndForget : SendingReliabilityStrategy
    {
        public override void SetupReliabilitySafeguards(SendingBusMessage message)
        {
            ReliabilityAchieved();
        }

        public override void RegisterAck(Guid messageId, string originatingPeer)
        {
        }



        public override event Action ReliabilityAchieved;
    }
}
using System;

namespace Bus.Transport.SendingPipe.SendingStrategies
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
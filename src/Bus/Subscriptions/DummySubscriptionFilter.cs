using System.Collections.Generic;
using Bus.InfrastructureMessages;
using Bus.InfrastructureMessages.Shadowing;
using Bus.MessageInterfaces;
using ProtoBuf;

namespace Bus.Subscriptions
{
    [ProtoContract]
    public class DummySubscriptionFilter : ISubscriptionFilter
    {
        public bool Matches(IMessage item)
        {
            return true;
        }
    }


    [ProtoContract]
    public class SynchronizeWithBrokerFilter : ISubscriptionFilter
    {
        [ProtoMember(1, IsRequired = true)]
        private readonly List<string> _acceptedPeers;

        public SynchronizeWithBrokerFilter(List<string> acceptedPeers)
        {
            _acceptedPeers = acceptedPeers;
        }

        private SynchronizeWithBrokerFilter() { }

        public bool Matches(IMessage item)
        {
            var syncMessage = item as SynchronizeWithBrokerCommand;

            if (syncMessage != null && _acceptedPeers != null)
                return _acceptedPeers.Contains(syncMessage.PeerName);

            var stopCommand = item as StopSynchWithBrokerCommand;
            if (stopCommand != null && _acceptedPeers != null)
                return _acceptedPeers.Contains(stopCommand.PeerName);

            return false;
        }
    }
}
using System.Collections.Generic;
using Bus.Attributes;
using Bus.MessageInterfaces;
using Bus.Transport;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Topology
{
    [ProtoContract]
    [InfrastructureMessage]
    class InitializeTopologyAndMessageSettings : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly List<ServicePeer> KnownPeers;
        [ProtoMember(2, IsRequired = true)]
        public readonly List<MessageOptions> MessageOptions;

        public InitializeTopologyAndMessageSettings(List<ServicePeer> knownPeers, List<MessageOptions> messageOptions)
        {
            KnownPeers = knownPeers;
            MessageOptions = messageOptions;
        }
    }
}
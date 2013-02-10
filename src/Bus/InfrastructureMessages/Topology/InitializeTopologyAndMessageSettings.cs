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

        public InitializeTopologyAndMessageSettings(List<ServicePeer> knownPeers)
        {
            KnownPeers = knownPeers;
        }
    }
}
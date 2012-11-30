using System;
using System.Collections.Generic;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ServicePeer))]
    public class InitializeTopologyAndMessageSettings : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly List<IServicePeer> KnownPeers;
        [ProtoMember(2, IsRequired = true)]
        public readonly List<MessageOptions> MessageOptions;

        public InitializeTopologyAndMessageSettings(List<IServicePeer> knownPeers, List<MessageOptions> messageOptions)
        {
            KnownPeers = knownPeers;
            MessageOptions = messageOptions;
        }
    }
}
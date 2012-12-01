﻿using ProtoBuf;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ServicePeer))]
    public class PeerConnected : IEvent
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly IServicePeer Peer;

        public PeerConnected(IServicePeer peer)
        {
            Peer = peer;
        }
    }
}
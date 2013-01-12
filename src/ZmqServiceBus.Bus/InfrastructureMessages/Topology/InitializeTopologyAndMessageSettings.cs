﻿using System;
using System.Collections.Generic;
using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [InfrastructureMessage]
    public class InitializeTopologyAndMessageSettings : ICommand
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
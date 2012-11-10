using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Commands
{
    public class InitializeTopologyAndMessageSettings : ICommand
    {
        public readonly List<ServicePeer> KnownPeers;
         public readonly Dictionary<Type, MessageOptions> MessageOptions;

        public InitializeTopologyAndMessageSettings(List<ServicePeer> knownPeers, Dictionary<Type, MessageOptions> messageOptions)
        {
            KnownPeers = knownPeers;
            MessageOptions = messageOptions;
        }
    }
}
using System.Collections.Generic;
using Shared;

namespace DirectoryService.Event
{
    public class PeerConnected
    {
        public readonly IServicePeer Peer;

        public PeerConnected(IServicePeer peer)
        {
            Peer = peer;
        }
    }
}
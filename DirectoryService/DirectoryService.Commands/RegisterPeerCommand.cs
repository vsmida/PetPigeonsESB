using ProtoBuf;
using Shared;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Commands
{
    [ProtoContract]
    public class RegisterPeerCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ServicePeer Peer;

        public RegisterPeerCommand(ServicePeer peer)
        {
            Peer = peer;
        }
    }
}
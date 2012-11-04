using ProtoBuf;
using Shared;

namespace DirectoryService.Commands
{
    [ProtoContract]
    public class RegisterPeer : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ServicePeer Peer;

        public RegisterPeer(ServicePeer peer)
        {
            Peer = peer;
        }
    }
}
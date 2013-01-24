using Bus.MessageInterfaces;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.InfrastructureMessages
{
    [ProtoContract]
    public class SynchronizeWithBrokerCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string PeerName;

        public SynchronizeWithBrokerCommand(string peerName)
        {
            PeerName = peerName;
        }
    }
}
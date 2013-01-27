using Bus.MessageInterfaces;
using ProtoBuf;

namespace Bus.InfrastructureMessages
{
    [ProtoContract]
    public class StopSynchWithBrokerCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string PeerName;

        public StopSynchWithBrokerCommand(string peerName)
        {
            PeerName = peerName;
        }
    }
}
using ProtoBuf;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class PersistMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public IReceivedTransportMessage Message;
    }
}

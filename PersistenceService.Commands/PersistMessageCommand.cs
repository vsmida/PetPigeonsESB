using ProtoBuf;
using Shared;
using ZmqServiceBus.Contracts;

namespace PersistenceService.Commands
{
    [ProtoContract]
    public class PersistMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public ITransportMessage Message;
    }
}

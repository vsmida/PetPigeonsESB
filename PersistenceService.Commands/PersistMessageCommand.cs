using ProtoBuf;
using Shared;

namespace PersistenceService.Commands
{
    [ProtoContract]
    public class PersistMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public TransportMessage Message;
    }
}

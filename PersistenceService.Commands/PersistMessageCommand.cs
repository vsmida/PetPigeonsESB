using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Transport;

namespace PersistenceService.Commands
{
    [ProtoContract]
    public class PersistMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public TransportMessage Message;
    }
}

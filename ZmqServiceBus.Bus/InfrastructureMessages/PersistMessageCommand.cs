using System;
using ProtoBuf;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    [ProtoInclude(1, typeof(SendingBusMessage))]
    public class PersistMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ISendingBusMessage Message;

        public PersistMessageCommand(ISendingBusMessage message)
        {
            Message = message;
        }
    }


    [ProtoContract]
    public class ForgetMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly Guid MessageId;

        public ForgetMessageCommand(string messageType, Guid messageId)
        {
            MessageType = messageType;
            MessageId = messageId;
        }
    }
}

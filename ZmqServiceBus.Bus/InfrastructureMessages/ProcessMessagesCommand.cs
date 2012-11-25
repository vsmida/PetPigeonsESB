using ProtoBuf;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Contracts;

namespace PersistenceService.Commands
{
    [ProtoContract]
    public class ProcessMessagesCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly string OriginatingPeer;
        [ProtoMember(3, IsRequired = true)]
        public readonly ReceivedTransportMessage[] MessagesToProcess;
        [ProtoMember(4, IsRequired = true)]
        public readonly bool IsEndOfQueue;

        public ProcessMessagesCommand(string messageType, string originatingPeer, ReceivedTransportMessage[] messagesToProcess, bool isEndOfQueue)
        {
            MessageType = messageType;
            OriginatingPeer = originatingPeer;
            MessagesToProcess = messagesToProcess;
            IsEndOfQueue = isEndOfQueue;
        }
    }
}
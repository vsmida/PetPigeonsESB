using ProtoBuf;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class ProcessMessageCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly ReceivedTransportMessage MessagesToProcess;

        public ProcessMessageCommand(ReceivedTransportMessage messagesToProcess)
        {
            MessagesToProcess = messagesToProcess;
        }
    }
}
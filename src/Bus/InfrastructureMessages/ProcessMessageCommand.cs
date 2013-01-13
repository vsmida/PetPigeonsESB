using Bus.MessageInterfaces;
using Bus.Transport.ReceptionPipe;
using ProtoBuf;

namespace Bus.InfrastructureMessages
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
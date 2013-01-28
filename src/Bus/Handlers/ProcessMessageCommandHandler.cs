using Bus.BusEventProcessorCommands;
using Bus.InfrastructureMessages;
using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.Handlers
{
    class ProcessMessageCommandHandler : ICommandHandler<ProcessMessageCommand>, ICommandHandler<EndOfPersistedMessages>
    {
        private readonly IDataReceiver _dataReceiver;

        public ProcessMessageCommandHandler(IDataReceiver dataReceiver)
        {
            _dataReceiver = dataReceiver;
        }

        public void Handle(ProcessMessageCommand item)
        {
            _dataReceiver.InjectMessage(item.MessagesToProcess, true);
        }

        public void Handle(EndOfPersistedMessages item)
        {
           // _dataReceiver.InjectCommand(new ReleaseCachedMessages());
        }
    }
}
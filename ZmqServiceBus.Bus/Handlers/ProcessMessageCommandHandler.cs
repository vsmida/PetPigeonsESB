using ZmqServiceBus.Bus.BusEventProcessorCommands;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Handlers
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
            _dataReceiver.InjectCommand(new ReleaseCachedMessages());
        }
    }
}
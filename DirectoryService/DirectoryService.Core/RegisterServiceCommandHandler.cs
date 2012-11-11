using DirectoryService.Commands;
using Shared;
using ZmqServiceBus.Bus;

namespace DirectoryService.Core
{
    public class RegisterServiceCommandHandler : ICommandHandler<RegisterPublisherCommand>, ICommandHandler<RegisterCommandHandlerCommand>
    {
        public void Handle(RegisterPublisherCommand command)
        {
            throw new System.NotImplementedException();
        }

        public void Handle(RegisterCommandHandlerCommand command)
        {
            throw new System.NotImplementedException();
        }
    }
}
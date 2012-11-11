using System;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Commands
{
    public class RegisterCommandHandlerCommand : ICommand
    {
        public readonly string Endpoint;
        public readonly Type CommandType;

        public RegisterCommandHandlerCommand(string endpoint, Type commandType)
        {
            Endpoint = endpoint;
            CommandType = commandType;
        }
    }
}
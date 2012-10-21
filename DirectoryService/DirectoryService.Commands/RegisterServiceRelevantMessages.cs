using System;
using Shared;

namespace DirectoryService.Commands
{
    public class RegisterServiceRelevantMessages : ICommand
    {
        public readonly string ServiceIdentity;
        public readonly string CommandsEndpoint;
        public readonly string EventsEndpoint;
        public readonly Type[] HandledCommands;
        public readonly Type[] SentEvents;
        public readonly Type[] CommandsSent;
        public readonly Type[] EventsListenedTo;
        
        public RegisterServiceRelevantMessages(string serviceIdentity, string commandsEndpoint, string eventsEndpoint, Type[] handledCommands, Type[] sentEvents, Type[] commandsSent, Type[] eventsListenedTo)
        {
            ServiceIdentity = serviceIdentity;
            HandledCommands = handledCommands;
            SentEvents = sentEvents;
            CommandsSent = commandsSent;
            EventsListenedTo = eventsListenedTo;
            CommandsEndpoint = commandsEndpoint;
            EventsEndpoint = eventsEndpoint;
        }
    }
}
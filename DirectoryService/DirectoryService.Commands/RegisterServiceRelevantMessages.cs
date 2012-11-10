using System;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Commands
{
    [ProtoContract]
    public class RegisterServiceRelevantMessages : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string ServiceIdentity;
        [ProtoMember(2, IsRequired = true)]
        public readonly string CommandsEndpoint;
        [ProtoMember(3, IsRequired = true)]
        public readonly string EventsEndpoint;
        [ProtoMember(4, IsRequired = true)]
        public readonly Type[] HandledCommands;
        [ProtoMember(5, IsRequired = true)]
        public readonly Type[] SentEvents;
        [ProtoMember(6, IsRequired = true)]
        public readonly Type[] EventsListenedTo;
        
        public RegisterServiceRelevantMessages(string serviceIdentity, string commandsEndpoint, string eventsEndpoint, Type[] handledCommands, Type[] sentEvents, Type[] eventsListenedTo)
        {
            ServiceIdentity = serviceIdentity;
            HandledCommands = handledCommands;
            SentEvents = sentEvents;
            EventsListenedTo = eventsListenedTo;
            CommandsEndpoint = commandsEndpoint;
            EventsEndpoint = eventsEndpoint;
        }
    }
}
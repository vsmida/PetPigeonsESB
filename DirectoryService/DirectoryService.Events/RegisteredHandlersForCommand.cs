using System;
using Shared;
using Shared.Attributes;

namespace DirectoryService.Event
{
    [InfrastructureMessage]
    public class RegisteredHandlersForCommand : IEvent
    {
        public readonly Type EventType;
        public readonly string[] Endpoints;

        public RegisteredHandlersForCommand(Type eventType, string[] endpoints)
        {
            EventType = eventType;
            Endpoints = endpoints;
        }
    }
}
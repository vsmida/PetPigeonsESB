using System;
using Shared;

namespace DirectoryService.Event
{
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
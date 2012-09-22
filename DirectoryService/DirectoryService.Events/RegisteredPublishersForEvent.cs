using System;
using Shared;

namespace DirectoryService.Event
{
    public class RegisteredPublishersForEvent : IEvent
    {
        public readonly Type CommandType;
        public readonly string[] Endpoints;

        public RegisteredPublishersForEvent(Type commandType, string[] endpoints)
        {
            CommandType = commandType;
            Endpoints = endpoints;
        }
    }
}
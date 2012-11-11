﻿using System;
using ZmqServiceBus.Contracts;

namespace DirectoryService.Commands
{
    public class RegisterPublisherCommand : ICommand
    {
        public readonly string Endpoint;
        public readonly Type EventType;

        public RegisterPublisherCommand(string endpoint, Type eventType)
        {
            Endpoint = endpoint;
            EventType = eventType;
        }
    }
}
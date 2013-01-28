﻿using System;

namespace Bus.InfrastructureMessages.Topology
{
    class MessageSettingsUpdated
    {
        public readonly Type MessageType;
        public readonly MessageOptions Options;

        public MessageSettingsUpdated(Type messageType, MessageOptions options)
        {
            MessageType = messageType;
            Options = options;
        }
    }
}
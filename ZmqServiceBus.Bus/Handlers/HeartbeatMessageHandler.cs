using System;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Handlers
{
    public class HeartbeatMessageHandler : ICommandHandler<HeartbeatRequest>, ICommandHandler<HeartbeatMessage>
    {
        private readonly IHeartbeatManager _heartbeatManager;
        private readonly IReplier _bus;

        public HeartbeatMessageHandler(IHeartbeatManager heartbeatManager, IReplier bus)
        {
            _heartbeatManager = heartbeatManager;
            _bus = bus;
        }

        public void Handle(HeartbeatRequest item)
        {
            _bus.Reply(new HeartbeatMessage(DateTime.UtcNow, item.Endpoint));
        }

        public void Handle(HeartbeatMessage item)
        {
            _heartbeatManager.RegisterHeartbeat(item);   
        }
    }
}
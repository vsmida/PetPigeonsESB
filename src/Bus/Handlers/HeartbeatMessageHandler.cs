using System;
using Bus.InfrastructureMessages.Heartbeating;
using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.Handlers
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
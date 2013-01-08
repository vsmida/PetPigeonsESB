using System;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class InboundInfrastructureEntry
    {
        public IMessage DeserializedMessage;
        public string SendingPeer;
        public Guid MessageIdentity;
        public bool ServiceInitialized;
        public WireTransportType TransportType;

    }
}
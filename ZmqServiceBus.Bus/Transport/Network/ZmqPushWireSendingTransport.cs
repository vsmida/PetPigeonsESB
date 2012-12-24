using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    class ZmqPushWireSendingTransport : IWireSendingTransport
    {
        public event Action<IEndpoint> EndpointDisconnected;
        public WireSendingTransportType TransportType { get { return WireSendingTransportType.ZmqPushTransport; } }
        private readonly Dictionary<ZmqEndpoint, ZmqSocket> _endpointsToSockets = new Dictionary<ZmqEndpoint, ZmqSocket>();
        private readonly ZmqContext _context;
        private readonly ZmqTransportConfiguration _zmqTransportConfiguration;


        public ZmqPushWireSendingTransport(ZmqContext context, ZmqTransportConfiguration zmqTransportConfiguration)
        {
            _context = context;
            _zmqTransportConfiguration = zmqTransportConfiguration;
        }

        public void Initialize()
        {

        }

        public void SendMessage(ISendingBusMessage message, IEndpoint endpoint)
        {
            ZmqSocket socket;
            var zmqEndpoint = (ZmqEndpoint)endpoint;
            if (!_endpointsToSockets.TryGetValue(zmqEndpoint, out socket))
            {
                socket = CreatePushSocket(zmqEndpoint);
                _endpointsToSockets.Add(zmqEndpoint, socket);
            }

            socket.SendMore(message.MessageType, Encoding.ASCII);
            socket.SendMore(_zmqTransportConfiguration.PeerName, Encoding.ASCII);
            socket.SendMore(message.MessageIdentity.ToByteArray());
            var sendStatus = socket.Send(message.Data, TimeSpan.FromMilliseconds(200));
            if (sendStatus != SendStatus.Sent) //peer is disconnected (or underwater from too many message), raise some event?
            {
                EndpointDisconnected(endpoint);
                //dispose socket and allow for re-creation of socket with same endpoint; everything will get slow as hell if we continue trying? or only if high water mark
                socket.Dispose();
                _endpointsToSockets.Remove(zmqEndpoint);
            }
        }

        private ZmqSocket CreatePushSocket(ZmqEndpoint zmqEndpoint)
        {
            var socket = _context.CreateSocket(SocketType.PUSH);
            socket.SendHighWatermark = 10000;
            socket.Linger = TimeSpan.FromMilliseconds(200);
            return socket;
        }

        public void SendMessage(ISendingBusMessage message, IEnumerable<IEndpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                SendMessage(message, endpoint);
            }
        }

        public void Dispose()
        {
            foreach (var socket in _endpointsToSockets.Values)
            {
                socket.Dispose();
            }
            _context.Dispose();
        }
    }
}
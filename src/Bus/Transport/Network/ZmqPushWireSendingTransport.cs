using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Bus.Dispatch;
using Bus.Serializer;
using Bus.Transport.SendingPipe;
using ZeroMQ;
using log4net;

namespace Bus.Transport.Network
{
    class ZmqPushWireSendingTransport : IWireSendingTransport
    {
        public event Action<IEndpoint> EndpointDisconnected = delegate { };
        public WireTransportType TransportType { get { return WireTransportType.ZmqPushPullTransport; } }
        private readonly Dictionary<ZmqEndpoint, ZmqSocket> _endpointsToSockets = new Dictionary<ZmqEndpoint, ZmqSocket>();
        private readonly ZmqContext _context;
        private readonly ILog _logger = LogManager.GetLogger(typeof(ZmqPushWireSendingTransport));
        private readonly MessageWireDataSerializer _serializer;


        public ZmqPushWireSendingTransport(ZmqContext context, ISerializationHelper helper)
        {
            _context = context;
            _serializer = new MessageWireDataSerializer(helper);
        }


        public void Initialize()
        {

        }

        public void SendMessage(WireSendingMessage message, IEndpoint endpoint)
        {
            ZmqSocket socket;
            var zmqEndpoint = (ZmqEndpoint)endpoint;
            if (!_endpointsToSockets.TryGetValue(zmqEndpoint, out socket))
            {
                socket = CreatePushSocket(zmqEndpoint);
                _endpointsToSockets.Add(zmqEndpoint, socket);
            }
            var status = SendStatus.TryAgain;
            var wait = default(SpinWait);
            bool first = true;
          //  _watch.Start();

            var buffer = _serializer.Serialize(message.MessageData);
            do
            {
                socket.Send(buffer, buffer.Length, SocketFlags.DontWait);
                status = socket.SendStatus;
                if (!first)
                    wait.SpinOnce();
                else
                    first = false;
            } while (status == SendStatus.TryAgain && wait.Count <1000);

            if (socket.SendStatus != SendStatus.Sent) //peer is disconnected (or underwater from too many message), raise some event?
            {
                _logger.Info(string.Format("disconnect of endpoint {0}", zmqEndpoint.Endpoint));
                EndpointDisconnected(endpoint);
                //dispose socket and allow for re-creation of socket with same endpoint; everything will get slow as hell if we continue trying? or only if high water mark
                socket.Dispose();
                _endpointsToSockets.Remove(zmqEndpoint);
            }
        }

        public void DisconnectEndpoint(IEndpoint endpoint)
        {
            _logger.Debug(string.Format("Disconnecting zmq endpoint {0}", endpoint));
            ZmqSocket socket;
            if (_endpointsToSockets.TryGetValue((ZmqEndpoint)endpoint, out socket))
            {
                socket.Dispose();
            }
        }

        private ZmqSocket CreatePushSocket(ZmqEndpoint zmqEndpoint)
        {
            _logger.Debug(string.Format("Creating zmq push socket to endpoint {0}", zmqEndpoint));
            var socket = _context.CreateSocket(SocketType.PUSH);
            socket.SendHighWatermark = 30000;
            socket.Linger = TimeSpan.FromMilliseconds(200);
            socket.Connect(zmqEndpoint.Endpoint);
            return socket;
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
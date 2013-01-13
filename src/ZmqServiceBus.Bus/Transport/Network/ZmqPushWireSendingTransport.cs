using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    class ZmqPushWireSendingTransport : IWireSendingTransport
    {
        public event Action<IEndpoint> EndpointDisconnected = delegate { };
        public WireTransportType TransportType { get { return WireTransportType.ZmqPushPullTransport; } }
        private readonly Dictionary<ZmqEndpoint, ZmqSocket> _endpointsToSockets = new Dictionary<ZmqEndpoint, ZmqSocket>();
        private readonly ZmqContext _context;


        public ZmqPushWireSendingTransport(ZmqContext context)
        {
            _context = context;
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
          //  socket.SendMore(message.MessageData.MessageType, Encoding.ASCII);
         //   socket.SendMore(message.MessageData.SendingPeer, Encoding.ASCII);
        //    socket.SendMore(message.MessageData.MessageIdentity.ToByteArray());
        //    socket.Send(message.MessageData.Data);
            Stopwatch watch = new Stopwatch();
            SendStatus status = SendStatus.TryAgain;
            watch.Start();
            while(status == SendStatus.TryAgain && watch.ElapsedMilliseconds <500)
            {
                status = socket.Send(BusSerializer.Serialize(message.MessageData), TimeSpan.FromMilliseconds(200));
                
            }
            watch.Stop();
            if (socket.SendStatus != SendStatus.Sent) //peer is disconnected (or underwater from too many message), raise some event?
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
            socket.SendHighWatermark = 20000;
            socket.Linger = TimeSpan.FromMilliseconds(200);
            socket.Connect(zmqEndpoint.Endpoint);
            return socket;
        }

        public void SendMessage(WireSendingMessage message, IEnumerable<IEndpoint> endpoints)
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
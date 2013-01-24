using System;
using System.Threading;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Shared;
using ZeroMQ;
using log4net;

namespace Bus.Transport.Network
{
    public class ZmqPullWireDataReceiver : IWireReceiverTransport
    {
        private readonly ZmqContext _context;
        private ZmqSocket _receptionSocket;
        private readonly Poller _receptionPoller = new Poller();
        private volatile bool _running = true;
        private Thread _pollingReceptionThread;
        private readonly ZmqTransportConfiguration _configuration;
        private RingBuffer<InboundMessageProcessingEntry> _ringBuffer;
        private ILog _logger = LogManager.GetLogger(typeof (ZmqPullWireDataReceiver));
        private ZmqEndpoint _endpoint;

        public ZmqPullWireDataReceiver(ZmqContext context, ZmqTransportConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        private void CreatePollingThread()
        {
            var socketsCreated = new AutoResetEvent(false);
            _pollingReceptionThread = new Thread(() =>
                                                               {
                                                                   CreateCommandReceiverSocket(_configuration.GetBindEndpoint());
                                                                   socketsCreated.Set();
                                                                   while (_running)
                                                                   {
                                                                       _receptionPoller.Poll(TimeSpan.FromMilliseconds(500));
                                                                   }
                                                                   _receptionSocket.Dispose();
                                                                   _receptionPoller.Dispose();
                                                               });

            _pollingReceptionThread.Start();
            socketsCreated.WaitOne();
            _endpoint = new ZmqEndpoint(_configuration.GetBindEndpoint());
        }

        public void CreateCommandReceiverSocket(string endpoint)
        {
            _receptionSocket = _context.CreateSocket(SocketType.PULL);
            _receptionSocket.Linger = TimeSpan.FromSeconds(1);
            _receptionSocket.ReceiveReady += (s, e) => ReceiveFromSocket(e);
            _receptionSocket.Bind(endpoint);
            _receptionPoller.AddSocket(_receptionSocket);
            _logger.DebugFormat("Command processor socket bound to {0}", endpoint);
        }


        private void ReceiveFromSocket(SocketEventArgs socketEventArgs)
        {
            var zmqSocket = socketEventArgs.Socket;
            //  var type = zmqSocket.Receive(Encoding.ASCII);
            //   var peerName = zmqSocket.Receive(Encoding.ASCII);

            //    var serializedId = zmqSocket.Receive();
            //     var messageId = new Guid(serializedId);
            //    var serializedItem = zmqSocket.Receive();
            try
            {
                var messagedata = BusSerializer.Deserialize<MessageWireData>(zmqSocket.Receive());

                //  var receivedTransportMessage = new ReceivedTransportMessage(type, peerName, messageId,TransportType, serializedItem);
                var receivedTransportMessage = new ReceivedTransportMessage(messagedata.MessageType, messagedata.SendingPeer, messagedata.MessageIdentity, _endpoint, messagedata.Data, messagedata.SequenceNumber);
                var sequence = _ringBuffer.Next();
                var entry = _ringBuffer[sequence];
                entry.InitialTransportMessage = receivedTransportMessage;
                entry.ForceMessageThrough = false;
                entry.Command = null;
                _ringBuffer.Publish(sequence);
            }
            catch(Exception e)
            {
                _logger.Error("Truncated zmq data received {0}", e);
            }
            
        }

        public void Dispose()
        {
            _running = false;
            if (_pollingReceptionThread != null)
                _pollingReceptionThread.Join();
            _context.Dispose();
        }

        public void Initialize(RingBuffer<InboundMessageProcessingEntry> ringBuffer)
        {
            _ringBuffer = ringBuffer;
            CreatePollingThread();
        }

        public WireTransportType TransportType
        {
            get { return WireTransportType.ZmqPushPullTransport; }
        }
    }
}
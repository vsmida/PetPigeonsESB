using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Bus.Dispatch;
using Bus.Serializer;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using Disruptor;
using Shared;
using ZeroMQ;
using log4net;

namespace Bus.Transport.Network
{
    class ZmqPullWireDataReceiver : IWireReceiverTransport
    {
        private readonly ZmqContext _context;
        private ZmqSocket _receptionSocket;
        private volatile bool _running = true;
        private Thread _pollingReceptionThread;
        private readonly ZmqTransportConfiguration _configuration;
        private RingBuffer<InboundMessageProcessingEntry> _ringBuffer;
        private ILog _logger = LogManager.GetLogger(typeof(ZmqPullWireDataReceiver));
        private ZmqEndpoint _endpoint;
        private readonly MessageWireDataSerializer _serializer;

        public ZmqPullWireDataReceiver(ZmqContext context, ZmqTransportConfiguration configuration, ISerializationHelper helper)
        {
            _context = context;
            _configuration = configuration;
            _serializer = new MessageWireDataSerializer(helper);
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
                                                                       DoReceive();
                                                                   }
                                                                   _receptionSocket.Dispose();
                                                               });

            _pollingReceptionThread.Start();
            socketsCreated.WaitOne();
            _endpoint = new ZmqEndpoint(_configuration.GetConnectEndpoint());
        }

        private void DoReceive()
        {
            try
            {
                var receive = _receptionSocket.Receive();
                if (receive.Length == 0)
                    return;

                //var messagedata = BusSerializer.Deserialize<MessageWireData>(receive);
                using (var stream = new MemoryStream(receive))
                {
                    var messagedata = _serializer.Deserialize(stream);


                    var sequence = _ringBuffer.Next();
                    var entry = _ringBuffer[sequence];
                    if (entry.InitialTransportMessage != null)
                        entry.InitialTransportMessage.Reinitialize(messagedata.MessageType,
                                                                   messagedata.SendingPeerId,
                                                                   messagedata.MessageIdentity,
                                                                   _endpoint,
                                                                   messagedata.Data,
                                                                   messagedata.SequenceNumber);
                    else
                    {
                        entry.InitialTransportMessage = new ReceivedTransportMessage(messagedata.MessageType,
                                                                                     messagedata.SendingPeerId,
                                                                                     messagedata.MessageIdentity,
                                                                                     _endpoint,
                                                                                     messagedata.Data,
                                                                                     messagedata.SequenceNumber);
                    }

                    //    entry.InitialTransportMessage = receivedTransportMessage;
                    entry.ForceMessageThrough = false;
                    entry.IsInfrastructureMessage = false;
                    entry.IsStrandardMessage = false;
                    entry.IsCommand = false;
                    entry.Command = null;
                    entry.QueuedInboundEntries = null;
                    // entry.InfrastructureEntry = null;
                    _ringBuffer.Publish(sequence);
                }
            }
            catch (Exception e)
            {
                _logger.Error("Truncated zmq data received {0}", e);
            }
        }

        public void CreateCommandReceiverSocket(string endpoint)
        {
            _receptionSocket = _context.CreateSocket(SocketType.PULL);
            _receptionSocket.Linger = TimeSpan.FromSeconds(1);
            _receptionSocket.ReceiveHighWatermark = 30000;
            _receptionSocket.Bind(endpoint);
            _logger.DebugFormat("Command processor socket bound to {0}", endpoint);
        }


        public void Dispose()
        {
            _running = false;
            _context.Dispose();
            if (_pollingReceptionThread != null)
                _pollingReceptionThread.Join();
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
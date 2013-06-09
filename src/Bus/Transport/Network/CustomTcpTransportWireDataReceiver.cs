using System;
using System.IO;
using System.Net;
using Bus.Serializer;
using Bus.Transport.ReceptionPipe;
using Disruptor;
using PgmTransport;
using Shared;
using log4net;

namespace Bus.Transport.Network
{
    class CustomTcpTransportWireDataReceiver : IWireReceiverTransport
    {
        private RingBuffer<InboundMessageProcessingEntry> _ringBuffer;
        private ILog _logger = LogManager.GetLogger(typeof(CustomTcpTransportWireDataReceiver));
        private readonly ICustomTcpTransportConfiguration _configuration;
        private readonly MessageWireDataSerializer _serializer;
        private readonly TcpReceiver _receiver = new TcpReceiver();
        private IPEndPoint _ipEndPoint;
        private CustomTcpWireDataReceiverEndpoint _endpoint;

        public CustomTcpTransportWireDataReceiver(ICustomTcpTransportConfiguration configuration, ISerializationHelper helper)
        {
            _configuration = configuration;
            _serializer = new MessageWireDataSerializer(helper);
            _endpoint = new CustomTcpWireDataReceiverEndpoint(new IPEndPoint(NetworkUtils.GetOwnIp(), _configuration.Port));
        }


        public void Dispose()
        {
            _receiver.StopListeningTo(_ipEndPoint);
            _receiver.Dispose();
        }

        public void Initialize(RingBuffer<InboundMessageProcessingEntry> ringBuffer)
        {
            _ringBuffer = ringBuffer;
            _ipEndPoint = new IPEndPoint(IPAddress.Loopback,_configuration.Port);
            _receiver.ListenToEndpoint(_ipEndPoint);
            _receiver.OnMessageReceived += DoReceive;
        }

        private void DoReceive(IPEndPoint arg1, Stream stream)
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

        public WireTransportType TransportType { get; private set; }
    }
}
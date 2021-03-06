using System;
using System.IO;
using System.Net;
using Bus.Serializer;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
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
        private CustomTcpEndpoint _endpoint;
        private readonly MessageWireData _messageWireData = new MessageWireData();

        public CustomTcpTransportWireDataReceiver(ICustomTcpTransportConfiguration configuration, ISerializationHelper helper)
        {
            _configuration = configuration;
            _serializer = new MessageWireDataSerializer(helper);
            _endpoint = new CustomTcpEndpoint(new IPEndPoint(IPAddress.Loopback, _configuration.Port));
        }


        public void Dispose()
        {
            _receiver.StopListeningTo(_ipEndPoint);
            _receiver.Dispose();
        }

        public void Initialize(RingBuffer<InboundMessageProcessingEntry> ringBuffer)
        {
            _ringBuffer = ringBuffer;
            _ipEndPoint = _endpoint.EndPoint;
            _receiver.RegisterCallback(_ipEndPoint, DoReceive);
            _receiver.ListenToEndpoint(_ipEndPoint);
        }

        private void DoReceive(Stream stream)
        {
            _serializer.Deserialize(stream, _messageWireData);

            var sequence = _ringBuffer.Next();
            var entry = _ringBuffer[sequence];
            if (entry.InitialTransportMessage != null)
                entry.InitialTransportMessage.Reinitialize(_messageWireData.MessageType,
                                                           _messageWireData.SendingPeerId,
                                                           _messageWireData.MessageIdentity,
                                                           _endpoint,
                                                           _messageWireData.Data,
                                                           _messageWireData.SequenceNumber);
            else
            {
                entry.InitialTransportMessage = new ReceivedTransportMessage(_messageWireData.MessageType,
                                                                             _messageWireData.SendingPeerId,
                                                                             _messageWireData.MessageIdentity,
                                                                             _endpoint,
                                                                             _messageWireData.Data,
                                                                             _messageWireData.SequenceNumber);
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
            stream.Dispose();
        }

        public WireTransportType TransportType { get; private set; }
    }
}
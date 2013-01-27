using System;
using System.Collections.Generic;
using Bus.BusEventProcessorCommands;
using Bus.MessageInterfaces;
using Bus.Transport.SendingPipe;
using Disruptor;
using System.Linq;
using log4net;
using log4net.Repository.Hierarchy;

namespace Bus.Transport.Network
{
    public interface INetworkSender : IDisposable
    {
        void Initialize();
    }

    public class NetworkSender : INetworkSender, IEventHandler<OutboundDisruptorEntry>
    {

        private class SequencingKey
        {
            public readonly IEndpoint Endpoint;
            public readonly string MessageType;

            public SequencingKey(IEndpoint endpoint, string messageType)
            {
                Endpoint = endpoint;
                MessageType = messageType;
            }

            protected bool Equals(SequencingKey other)
            {
                return Equals(Endpoint, other.Endpoint) && string.Equals(MessageType, other.MessageType);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((SequencingKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Endpoint != null ? Endpoint.GetHashCode() : 0)*397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
                }
            }
        }

        private Dictionary<WireTransportType, IWireSendingTransport> _wireSendingTransports;
        private readonly IHeartbeatManager _heartbeatManager;
        private readonly Dictionary<SequencingKey, int> _endpointToSequenceNumber = new Dictionary<SequencingKey, int>();
        private ILog _logger = LogManager.GetLogger(typeof (NetworkSender));

        public NetworkSender(IWireSendingTransport[] wireSendingTransports, IHeartbeatManager heartbeatManager)
        {
            _heartbeatManager = heartbeatManager;
            _wireSendingTransports = wireSendingTransports.ToDictionary(x => x.TransportType, x => x);
            foreach (var transport in wireSendingTransports)
            {
                transport.EndpointDisconnected += OnEndpointDisconnected;
            }

        }

        private void OnEndpointDisconnected(IEndpoint obj)
        {
            _logger.Debug(string.Format("Endpoint disconnected {0}", obj));
            foreach (var key in _endpointToSequenceNumber.Keys.Where(x => x.Endpoint == obj).ToList())
            {
                _endpointToSequenceNumber[key] = 0;
            }
        }

        public void Dispose()
        {
            foreach (var wireSendingTransport in _wireSendingTransports.Values)
            {
                _logger.Debug(string.Format("disposing transport type {0}", wireSendingTransport.TransportType));
                wireSendingTransport.Dispose();
            }
        }

        public void Initialize()
        {
            foreach (var wireSendingTransport in _wireSendingTransports.Values)
            {
                wireSendingTransport.Initialize();
            }

            _heartbeatManager.Initialize();
        }

        private void SendMessageInternal(WireSendingMessage message)
        {
            var endpoint = message.Endpoint;
            var key = new SequencingKey(endpoint, message.MessageData.MessageType);
            _heartbeatManager.StartMonitoring(endpoint);
            _wireSendingTransports[endpoint.WireTransportType].SendMessage(message, endpoint);
            //if(message.Sequenced)
            //{
            //    int sequenceNumber;
            //    if (!_endpointToSequenceNumber.TryGetValue(key, out sequenceNumber))
            //        _endpointToSequenceNumber.Add(key, sequenceNumber);
            //    message.MessageData.SequenceNumber = sequenceNumber;
            //    _wireSendingTransports[endpoint.WireTransportType].SendMessage(message, endpoint);
            //    _endpointToSequenceNumber[key]++;
            //}
            //else
            //{
            //    message.MessageData.SequenceNumber = null;
            //    _wireSendingTransports[endpoint.WireTransportType].SendMessage(message, endpoint);
                
            //}

           
        }

        public void OnNext(OutboundDisruptorEntry data, long sequence, bool endOfBatch)
        {
            if (data.NetworkSenderData.Command != null)
                HandleCommand(data.NetworkSenderData.Command);

            foreach (var wireSendingMessage in data.NetworkSenderData.WireMessages)
            {
                SendMessageInternal(wireSendingMessage);
            }
        }

        private void HandleCommand(IBusEventProcessorCommand command)
        {
            if (command is DisconnectEndpoint)
            {
                var typedCommand = (DisconnectEndpoint)command;
                _logger.Debug(string.Format("handling disconnect endpoint command {0}", typedCommand.Endpoint));
                foreach (var key in _endpointToSequenceNumber.Keys.Where(x => x.Endpoint == typedCommand.Endpoint).ToList())
                {
                    _endpointToSequenceNumber[key] = 0;
                }

            }
        }
    }
}
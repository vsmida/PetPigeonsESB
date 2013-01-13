using System;
using System.Collections.Generic;
using Bus.Transport.SendingPipe;
using Disruptor;
using System.Linq;

namespace Bus.Transport.Network
{
    public interface INetworkSender : IDisposable
    {
        void Initialize();
    }

    public class NetworkSender : INetworkSender, IEventHandler<OutboundDisruptorEntry>
    {
        private Dictionary<WireTransportType, IWireSendingTransport> _wireSendingTransports;
        private readonly IHeartbeatManager _heartbeatManager;


        public NetworkSender(IWireSendingTransport[] wireSendingTransports, IHeartbeatManager heartbeatManager)
        {
            _heartbeatManager = heartbeatManager;
            _wireSendingTransports = wireSendingTransports.ToDictionary(x => x.TransportType, x => x);

        }
        public void Dispose()
        {
            foreach (var wireSendingTransport in _wireSendingTransports.Values)
            {
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
            _heartbeatManager.StartMonitoring(endpoint);
            _wireSendingTransports[endpoint.WireTransportType].SendMessage(message, endpoint);

        }

        public void OnNext(OutboundDisruptorEntry data, long sequence, bool endOfBatch)
        {
            foreach (var wireSendingMessage in data.NetworkSenderData.WireMessages)
            {
                SendMessageInternal(wireSendingMessage);
            }
        }
    }
}
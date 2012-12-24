using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using System.Linq;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IDataSender : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingBusMessage message, IEndpoint endpoint);
        void SendMessage(ISendingBusMessage message, IEnumerable<IEndpoint> endpoint);
    }

    public class DataSender : IDataSender
    {
        private Dictionary<WireSendingTransportType, IWireSendingTransport> _wireSendingTransports;

        public DataSender(IWireSendingTransport[] wireSendingTransports)
        {
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
        }

        public void SendMessage(ISendingBusMessage message, IEndpoint endpoint)
        {
            _wireSendingTransports[endpoint.WireTransportType].SendMessage(message, endpoint);
        }

        public void SendMessage(ISendingBusMessage message, IEnumerable<IEndpoint> endpoints)
        {
            foreach (var endpointByTransport in endpoints.GroupBy(x => x.WireTransportType))
            {
                _wireSendingTransports[endpointByTransport.Key].SendMessage(message, endpointByTransport);
            }
        }
    }
}
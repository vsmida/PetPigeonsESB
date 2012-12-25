using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared;
using ZeroMQ.Monitoring;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IDataReceiver : IDisposable
    {
        event Action<IReceivedTransportMessage> OnMessageReceived;
        void Initialize();
    }

    public class DataReceiver : IDataReceiver
    {
        private readonly IWireReceiverTransport[] _transports;
        public event Action<IReceivedTransportMessage> OnMessageReceived;
        private readonly BlockingCollection<IReceivedTransportMessage> _messagesToForward = new BlockingCollection<IReceivedTransportMessage>();


        public DataReceiver(IWireReceiverTransport[] transports)
        {
            _transports = transports;
        }

        public void Initialize()
        {
            foreach (IWireReceiverTransport wireReceiverTransport in _transports)
            {
                wireReceiverTransport.Initialize(_messagesToForward);
            }
            CreateDequeueThread();
        }


        private void CreateDequeueThread()
        {
            new BackgroundThread(() =>
            {
                foreach (var message in _messagesToForward.GetConsumingEnumerable())
                {
                    OnMessageReceived(message);
                }
            }).Start();
        }

        public void Dispose()
        {
            foreach (IWireReceiverTransport wireReceiverTransport in _transports)
            {
                wireReceiverTransport.Dispose();
            }
            _messagesToForward.CompleteAdding();
        }
    }
}
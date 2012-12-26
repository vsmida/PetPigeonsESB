using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
        private Thread _dequeueThread;


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
            _dequeueThread = new Thread(() =>
                                            {
                                                foreach (var message in _messagesToForward.GetConsumingEnumerable())
                                                {
                                                    OnMessageReceived(message);
                                                }
                                            });
            _dequeueThread.Start();
        }

        public void Dispose()
        {
            foreach (IWireReceiverTransport wireReceiverTransport in _transports)
            {
                wireReceiverTransport.Dispose();
            }
            _messagesToForward.CompleteAdding();
            _dequeueThread.Join();
        }
    }
}
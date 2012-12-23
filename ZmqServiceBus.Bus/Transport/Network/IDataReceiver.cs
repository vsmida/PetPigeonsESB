using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Shared;
using ZeroMQ;
using ZeroMQ.Monitoring;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IDataReceiver : IDisposable
    {
        event Action<IReceivedTransportMessage> OnMessageReceived;
        void Initialize();
    }

    public class ZmqDataReceiver : IDataReceiver
    {
        public event Action<IReceivedTransportMessage> OnMessageReceived;
        private readonly ZmqContext _context;
        private ZmqSocket receptionSocket;
        private readonly Poller _receptionPoller = new Poller();
        private volatile bool _running = true;
        private BackgroundThread _pollingReceptionThread;
        private readonly BlockingCollection<IReceivedTransportMessage> _messagesToForward = new BlockingCollection<IReceivedTransportMessage>();
        private readonly TransportConfiguration _configuration;

        public ZmqDataReceiver(ZmqContext context, TransportConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        public void Initialize()
        {
            CreatePollingThread();
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

        private void CreatePollingThread()
        {
            var socketsCreated = new AutoResetEvent(false);
            _pollingReceptionThread = new BackgroundThread(() =>
                                                               {
                                                                   CreateCommandReceiverSocket(_configuration.GetCommandsBindEnpoint());
                                                                   socketsCreated.Set();
                                                                   while (_running)
                                                                   {
                                                                       _receptionPoller.Poll(TimeSpan.FromMilliseconds(500));
                                                                   }
                                                                   _receptionPoller.Dispose();
                                                               });

            _pollingReceptionThread.Start();
            socketsCreated.WaitOne();
        }

        public void CreateCommandReceiverSocket(string endpoint)
        {
            receptionSocket = _context.CreateSocket(SocketType.SUB);
            receptionSocket.Linger = TimeSpan.FromSeconds(1);
            receptionSocket.ReceiveReady += (s, e) => ReceiveFromSocket(e);
            receptionSocket.Bind(endpoint);
            receptionSocket.SubscribeAll();
            _receptionPoller.AddSocket(receptionSocket);
            Console.WriteLine("Command processor socket bound to {0}", endpoint);
        }


        private void ReceiveFromSocket(SocketEventArgs socketEventArgs)
        {
            var zmqSocket = socketEventArgs.Socket;
            var type = zmqSocket.Receive(Encoding.ASCII);
            var peerName = zmqSocket.Receive(Encoding.ASCII);
            var serializedId = zmqSocket.Receive();
            var messageId = new Guid(serializedId);
            var serializedItem = zmqSocket.Receive();
            _messagesToForward.Add(new ReceptionPipe.ReceivedTransportMessage(type, peerName, messageId, serializedItem));

        }

        public void Dispose()
        {
            _running = false;
            if (_pollingReceptionThread != null)
                _pollingReceptionThread.Join();
            receptionSocket.Dispose();
            _messagesToForward.CompleteAdding();
        }
    }
}
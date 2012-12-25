using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public class ZmqDataReceiver : IWireReceiverTransport
    {
        private readonly ZmqContext _context;
        private ZmqSocket receptionSocket;
        private readonly Poller _receptionPoller = new Poller();
        private volatile bool _running = true;
        private BackgroundThread _pollingReceptionThread;
        private readonly ZmqTransportConfiguration _configuration;
        private BlockingCollection<IReceivedTransportMessage> _messagesQueue;

        public ZmqDataReceiver(ZmqContext context, ZmqTransportConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        private void CreatePollingThread()
        {
            var socketsCreated = new AutoResetEvent(false);
            _pollingReceptionThread = new BackgroundThread(() =>
                                                               {
                                                                   CreateCommandReceiverSocket(_configuration.GetBindEndpoint());
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
            receptionSocket = _context.CreateSocket(SocketType.PULL);
            receptionSocket.Linger = TimeSpan.FromSeconds(1);
            receptionSocket.ReceiveReady += (s, e) => ReceiveFromSocket(e);
            receptionSocket.Bind(endpoint);
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
            _messagesQueue.TryAdd(new ReceivedTransportMessage(type, peerName, messageId, serializedItem));

        }

        public void Dispose()
        {
            _running = false;
            if (_pollingReceptionThread != null)
                _pollingReceptionThread.Join();
            receptionSocket.Dispose();
        }

        public void Initialize(BlockingCollection<IReceivedTransportMessage> messageQueue)
        {
            _messagesQueue = messageQueue;
            CreatePollingThread();
        }
    }
}
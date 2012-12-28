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
        private ZmqSocket _receptionSocket;
        private readonly Poller _receptionPoller = new Poller();
        private volatile bool _running = true;
        private Thread _pollingReceptionThread;
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
            _pollingReceptionThread = new Thread(() =>
                                                               {
                                                                   CreateCommandReceiverSocket(_configuration.GetBindEndpoint());
                                                                   socketsCreated.Set();
                                                                   while (_running)
                                                                   {
                                                                       _receptionPoller.Poll(TimeSpan.FromMilliseconds(500));
                                                                   }
                                                                   _receptionSocket.Dispose();
                                                                   _receptionPoller.Dispose();
                                                               });

            _pollingReceptionThread.Start();
            socketsCreated.WaitOne();
        }

        public void CreateCommandReceiverSocket(string endpoint)
        {
            _receptionSocket = _context.CreateSocket(SocketType.PULL);
            _receptionSocket.Linger = TimeSpan.FromSeconds(1);
            _receptionSocket.ReceiveReady += (s, e) => ReceiveFromSocket(e);
            _receptionSocket.Bind(endpoint);
            _receptionPoller.AddSocket(_receptionSocket);
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
            _context.Dispose();
        }

        public void Initialize(BlockingCollection<IReceivedTransportMessage> messageQueue)
        {
            _messagesQueue = messageQueue;
            CreatePollingThread();
        }
    }
}
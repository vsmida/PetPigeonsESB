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

    public class DataReceiver : IDataReceiver
    {
        public event Action<IReceivedTransportMessage> OnMessageReceived;
        private readonly ZmqContext _context;
        private ZmqSocket _commandReceptionSocket;
        private ZmqSocket _eventReceptionSocket;
        private readonly Poller _receptionPoller = new Poller();
        private volatile bool _running = true;
        private BackgroundThread _pollingReceptionThread;
        private readonly BlockingCollection<Action> _actionsToPerformOnPollingThread = new BlockingCollection<Action>();
        private readonly BlockingCollection<IReceivedTransportMessage> _messagesToForward = new BlockingCollection<IReceivedTransportMessage>();
        private readonly TransportConfiguration _configuration;

        public DataReceiver(ZmqContext context, TransportConfiguration configuration)
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

        private void SubscribeToEventPublisher(string publisherEndpoint, string typeName)
        {
            _eventReceptionSocket.Connect(publisherEndpoint);
            _eventReceptionSocket.Subscribe(Encoding.ASCII.GetBytes(typeName));
        }

        private void CreatePollingThread()
        {
            AutoResetEvent socketsCreated = new AutoResetEvent(false);
            _pollingReceptionThread = new BackgroundThread(() =>
                                                               {
                                                                   CreateCommandReceiverSocket(_configuration.GetCommandsBindEnpoint());
                                                                   CreateSubscribeSocket();
                                                                   socketsCreated.Set();
                                                                   while (_running)
                                                                   {
                                                                       _receptionPoller.Poll(TimeSpan.FromMilliseconds(500));
                                                                       Action toDo;
                                                                       if (_actionsToPerformOnPollingThread.TryTake(out toDo))
                                                                       {
                                                                           toDo();
                                                                       }

                                                                   }
                                                                   _receptionPoller.Dispose();
                                                               });

            _pollingReceptionThread.Start();
            socketsCreated.WaitOne();
        }

        public void CreateCommandReceiverSocket(string endpoint)
        {
            _commandReceptionSocket = _context.CreateSocket(SocketType.SUB);
            _commandReceptionSocket.Linger = TimeSpan.FromSeconds(1);
            _commandReceptionSocket.Identity = Encoding.ASCII.GetBytes(_configuration.PeerName);
            _commandReceptionSocket.ReceiveReady += (s, e) => ReceiveFromCommandSocket(e);
            _receptionPoller.AddSocket(_commandReceptionSocket);
            _commandReceptionSocket.Bind(endpoint);
            Console.WriteLine("Command processor socket bound to {0}", endpoint);
        }


        public void CreateSubscribeSocket()
        {
            _eventReceptionSocket = _context.CreateSocket(SocketType.SUB);
            _eventReceptionSocket.Linger = TimeSpan.FromSeconds(1);

            _eventReceptionSocket.ReceiveReady += (s, e) => ReceiveFromEventSubscriber(e);
            _receptionPoller.AddSocket(_eventReceptionSocket);
        }

        private void ReceiveFromEventSubscriber(SocketEventArgs socketEventArgs)
        {
            var zmqSocket = socketEventArgs.Socket;
            var type = zmqSocket.Receive(Encoding.ASCII);
            var senderServiceId = zmqSocket.Receive(Encoding.ASCII);
            var id = new Guid(zmqSocket.Receive());
            var data = zmqSocket.Receive();
            _messagesToForward.Add(new ReceptionPipe.ReceivedTransportMessage(type, senderServiceId, id, data));
        }

        private void ReceiveFromCommandSocket(SocketEventArgs socketEventArgs)
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
            _commandReceptionSocket.Dispose();
            _eventReceptionSocket.Dispose();
            _messagesToForward.CompleteAdding();
        }
    }
}
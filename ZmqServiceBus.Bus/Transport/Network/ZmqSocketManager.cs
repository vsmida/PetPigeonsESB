using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public class ZmqSocketManager : IZmqSocketManager
    {
        private readonly ZmqContext _context;
        private volatile bool _running = true;
        private readonly List<ZmqSocket> _socketsToDispose = new List<ZmqSocket>();
        private readonly Poller _poller = new Poller();
        private BackgroundThread _pollingThread;
        private ZmqSocket _subSocket;

        public ZmqSocketManager(ZmqContext context)
        {
            _context = context;
        }

        private void CreatePollingThread()
        {
            _pollingThread = new BackgroundThread(() =>
                                                      {
                                                          while (_running)
                                                              _poller.Poll(TimeSpan.FromMilliseconds(10));

                                                          _poller.Dispose();
                                                      });
            _pollingThread.Start();
        }


        public void SubscribeTo(string endpoint, string messageType)
        {
            _subSocket.Connect(endpoint);
            _subSocket.Subscribe(Encoding.ASCII.GetBytes(messageType));
        }

        public void CreateSubscribeSocket(BlockingCollection<IReceivedTransportMessage> receiveQueue)
        {
            _subSocket = _context.CreateSocket(SocketType.SUB);
            _socketsToDispose.Add(_subSocket);
            _subSocket.Linger = TimeSpan.FromSeconds(1);

            _subSocket.ReceiveReady += (s, e) => ReceiveFromSubscriber(e, receiveQueue);
            _poller.AddSocket(_subSocket);
            if (_pollingThread == null)
                CreatePollingThread();
        }

        private void ReceiveFromSubscriber(SocketEventArgs socketEventArgs, BlockingCollection<IReceivedTransportMessage> receiveQueue)
        {
            var zmqSocket = socketEventArgs.Socket;
            var type = zmqSocket.Receive(Encoding.ASCII);
            var senderServiceId = zmqSocket.Receive(Encoding.ASCII);
            var id = new Guid(zmqSocket.Receive());
            var data = zmqSocket.Receive();
            receiveQueue.Add(new ReceptionPipe.ReceivedTransportMessage(type, senderServiceId, id, data));
        }


        public void CreateRequestSocket(BlockingCollection<ISendingTransportMessage> sendingQueue, BlockingCollection<IReceivedTransportMessage> acknowledgementQueue, string endpoint, string servicePeerName)
        {
            var requestorSocket = _context.CreateSocket(SocketType.DEALER);
            requestorSocket.Linger = TimeSpan.FromSeconds(1);
            requestorSocket.SendHighWatermark = 10000;
            requestorSocket.ReceiveHighWatermark = 10000;
            _socketsToDispose.Add(requestorSocket);
            requestorSocket.ReceiveReady += (s, e) => ReceiveFromDealer(e, acknowledgementQueue);
            requestorSocket.SendReady += (s, e) => SendWithoutIdentity(e, sendingQueue, servicePeerName);
            _poller.AddSocket(requestorSocket);
            requestorSocket.Connect(endpoint);
            Console.WriteLine("Command dealer socket bound to {0}", endpoint);
            if (_pollingThread == null)
                CreatePollingThread();
        }

        private void SendWithoutIdentity(SocketEventArgs socketEventArgs, BlockingCollection<ISendingTransportMessage> sendingQueue, string servicePeerName)
        {
            ISendingTransportMessage message;
            if (!sendingQueue.TryTake(out message))
                return;
            var zmqSocket = socketEventArgs.Socket;

            zmqSocket.SendMore(Encoding.ASCII.GetBytes(message.MessageType));
            zmqSocket.SendMore(Encoding.ASCII.GetBytes(servicePeerName));
            zmqSocket.SendMore(message.MessageIdentity.ToByteArray());
            zmqSocket.Send(message.Data);
        }

        private void ReceiveFromDealer(SocketEventArgs socketEventArgs, BlockingCollection<IReceivedTransportMessage> acknowledgementQueue)
        {
            var zmqSocket = socketEventArgs.Socket;
            zmqSocket.Receive();
            var type = zmqSocket.Receive(Encoding.ASCII);
            var servicePeerName = zmqSocket.Receive(Encoding.ASCII);
            var id = new Guid(zmqSocket.Receive());
            var serializedItem = zmqSocket.Receive();
            acknowledgementQueue.Add(new ReceptionPipe.ReceivedTransportMessage(type,servicePeerName,id, serializedItem));
        }

        public void CreatePublisherSocket(BlockingCollection<ISendingTransportMessage> sendingQueue, string endpoint, string servicePeerName)
        {
            var waitForBind = new AutoResetEvent(false);
            new BackgroundThread(() =>
                                     {
                                         var publisherSocket = _context.CreateSocket(SocketType.PUB);
                                         publisherSocket.Linger = TimeSpan.FromSeconds(1);
                                         publisherSocket.SendHighWatermark = 10000;
                                         publisherSocket.Bind(endpoint);
                                         waitForBind.Set();
                                         while (_running)
                                         {
                                             ISendingTransportMessage message;
                                             if (sendingQueue.TryTake(out message, TimeSpan.FromSeconds(0.1)))
                                             {
                                                 publisherSocket.SendMore(Encoding.ASCII.GetBytes(message.MessageType));
                                                 publisherSocket.SendMore(Encoding.ASCII.GetBytes(servicePeerName));
                                                 publisherSocket.SendMore(message.MessageIdentity.ToByteArray());
                                                 publisherSocket.Send(message.Data);
                                             }
                                         }
                                         publisherSocket.Dispose();
                                     }).Start();
            waitForBind.WaitOne();

        }

        public void CreateResponseSocket(BlockingCollection<IReceivedTransportMessage> receivingQueue, string endpoint, string servicePeerName)
        {
            var replierSocket = _context.CreateSocket(SocketType.ROUTER);
            replierSocket.Linger = TimeSpan.FromSeconds(1);
            replierSocket.SendHighWatermark = 10000;
            replierSocket.ReceiveHighWatermark = 10000;
            _socketsToDispose.Add(replierSocket);
            replierSocket.ReceiveReady += (s, e) => ReceiveFromRouter(e, receivingQueue, servicePeerName);
            _poller.AddSocket(replierSocket);
            replierSocket.Bind(endpoint);
            Console.WriteLine("Command replier socket bound to {0}", endpoint);
            if (_pollingThread == null)
                CreatePollingThread();
        }

        private void ReceiveFromRouter(SocketEventArgs socketEventArgs, BlockingCollection<IReceivedTransportMessage> receivingQueue, string servicePeerName)
        {
            var zmqSocket = socketEventArgs.Socket;
            var zmqIdentity = zmqSocket.Receive();
            var type = zmqSocket.Receive(Encoding.ASCII);
            var peerName = zmqSocket.Receive(Encoding.ASCII);
            var serializedId = zmqSocket.Receive();
            var messageId = new Guid(serializedId);
            var serializedItem = zmqSocket.Receive();
            receivingQueue.Add(new ReceptionPipe.ReceivedTransportMessage(type,peerName, messageId, serializedItem));

            if (type == typeof(ReceivedOnTransportAcknowledgement).FullName)
                return;
            zmqSocket.SendMore(zmqIdentity);
            zmqSocket.SendMore(new byte[0]);
            zmqSocket.SendMore(Encoding.ASCII.GetBytes(typeof(ReceivedOnTransportAcknowledgement).FullName));
            zmqSocket.SendMore(Encoding.ASCII.GetBytes(servicePeerName));
            zmqSocket.SendMore(messageId.ToByteArray());
            zmqSocket.Send(new byte[0]);

        }

        public void Stop()
        {
            _running = false;
            if (_pollingThread != null)
                _pollingThread.Join();
            foreach (var zmqSocket in _socketsToDispose)
            {
                zmqSocket.Dispose();
            }
        }
    }
}
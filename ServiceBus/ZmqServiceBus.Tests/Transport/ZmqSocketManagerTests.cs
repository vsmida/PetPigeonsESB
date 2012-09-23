using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class ZmqSocketManagerTests
    {
        private const string Endpoint = "inproc://test";
        private ZmqSocketManager _socketManager;
        private ZmqContext _zmqContext;

        [SetUp]
        public void Setup()
        {
            _zmqContext = ZmqContext.Create();
            _socketManager = new ZmqSocketManager(_zmqContext);
        }

        [Test, Timeout(1000), Repeat(10)]
        public void should_create_requestor_socket()
        {
            var sendCollection = new BlockingCollection<ITransportMessage>();
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            string senderIdentity = "Identity";
            var commandType = "Request";
            var commandData = "Data";
            var ackType = "Ack";
            var ackData = "None";
            var routerSocket = _zmqContext.CreateSocket(SocketType.ROUTER);
            routerSocket.Bind(Endpoint);
            _socketManager.CreateRequestSocket(sendCollection, receiveCollection, Endpoint, senderIdentity);
            sendCollection.Add(new TransportMessage(senderIdentity, commandType, Encoding.ASCII.GetBytes(commandData)));

            Assert.AreEqual(senderIdentity, Encoding.ASCII.GetString(routerSocket.Receive()));
            Assert.AreEqual(commandType, Encoding.ASCII.GetString(routerSocket.Receive()));
            Assert.AreEqual(commandData, Encoding.ASCII.GetString(routerSocket.Receive()));

            routerSocket.SendMore(Encoding.ASCII.GetBytes(senderIdentity));
            routerSocket.SendMore(new byte[0]);
            routerSocket.SendMore(Encoding.ASCII.GetBytes(ackType));
            routerSocket.Send(Encoding.ASCII.GetBytes(ackData));

            var receivedAck = receiveCollection.Take();
            Assert.AreEqual(ackType, receivedAck.MessageType);
            Assert.AreEqual(Encoding.ASCII.GetBytes(ackData), receivedAck.Data);

            routerSocket.Dispose();
        }

        [Test, Timeout(1000), Repeat(10)]
        public void should_create_publisher_socket()
        {
            var sendQueue = new BlockingCollection<ITransportMessage>();
            var subscriberSocket = _zmqContext.CreateSocket(SocketType.SUB);
            var messageType = "Type";
            var messageData = "Data";
            subscriberSocket.SubscribeAll();
            _socketManager.CreatePublisherSocket(sendQueue, Endpoint);
            subscriberSocket.Connect(Endpoint);

            var transportMessage = new TransportMessage(null, messageType, Encoding.ASCII.GetBytes(messageData));
            sendQueue.Add(transportMessage);

            Assert.AreEqual(Encoding.ASCII.GetBytes(transportMessage.MessageType), subscriberSocket.Receive());
            Assert.AreEqual(transportMessage.Data, subscriberSocket.Receive());
            subscriberSocket.Dispose();
        }

        [Test, Timeout(1000), Repeat(5)]
        public void should_create_subscriber_socket()
        {
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            var publisherSocket = _zmqContext.CreateSocket(SocketType.PUB);
            publisherSocket.Bind(Endpoint);
            _socketManager.CreateSubscribeSocket(receiveCollection, Endpoint);
            var messageType = "Type";
            var messageData = "Data";
            publisherSocket.SendMore(Encoding.ASCII.GetBytes(messageType));
            publisherSocket.Send(Encoding.ASCII.GetBytes(messageData));

            var receivedMessage = receiveCollection.Take();
            Assert.AreEqual(messageType, receivedMessage.MessageType);
            Assert.AreEqual(Encoding.ASCII.GetBytes(messageData), receivedMessage.Data);

            publisherSocket.Dispose();
        }

        [Test, Repeat(5)]
        public void should_not_drop_messages_or_block_sending_when_one_consumer_dies()
        {
            var sendCollection = new BlockingCollection<ITransportMessage>();
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            const string ephemeralSenderIdentity = "Temp";
            const string durableSenderIdentity = "Identity";
            const int maxMessages = 12000; //HWM + 2000

            _socketManager.CreateResponseSocket(receiveCollection, sendCollection, Endpoint, "ReplierIdentity");
            var ephemeralRequestorSocket = CreateEphemeralSocketAndSendData(ephemeralSenderIdentity, Endpoint);
            var receiveThread = StartReceivingThread(durableSenderIdentity, Endpoint, maxMessages);
            ephemeralRequestorSocket.Dispose();

            for (int i = 0; i < maxMessages; i++)
            {
                sendCollection.Add(new TransportMessage(ephemeralSenderIdentity, "Garbage", new byte[1000]));
                sendCollection.Add(new TransportMessage(durableSenderIdentity, "Garbage", Encoding.ASCII.GetBytes(i.ToString())));
            }
            receiveThread.Join();

        }

        private ZmqSocket CreateEphemeralSocketAndSendData(string ephemeralSenderIdentity, string endpoint)
        {
            var ephemeralRequestorSocket = CreateRequestorSocket(ephemeralSenderIdentity, endpoint);
            ephemeralRequestorSocket.SendMore(Encoding.ASCII.GetBytes("Type"));
            ephemeralRequestorSocket.Send(Encoding.ASCII.GetBytes("Data"));
            return ephemeralRequestorSocket;
        }

        private BackgroundThread StartReceivingThread(string durableSenderIdentity, string endpoint, int maxMessages)
        {
            var waitForConnect = new AutoResetEvent(false);
            var receiveThread = new BackgroundThread(() =>
                                                         {
                                                             var durableSocket = CreateRequestorSocket(durableSenderIdentity,
                                                                                                       endpoint);
                                                             waitForConnect.Set();
                                                             for (int i = 0; i < maxMessages; i++)
                                                             {
                                                                 durableSocket.Receive();
                                                                 durableSocket.Receive();
                                                                 Assert.AreEqual(i.ToString(), durableSocket.Receive(Encoding.ASCII));
                                                             }
                                                             durableSocket.Dispose();
                                                         });
            receiveThread.Start();
            waitForConnect.WaitOne();
            return receiveThread;
        }

        [Test, Timeout(1000), Repeat(10)]
        public void should_receive_messages_on_replier_socket_and_route_messages_back()
        {
            var sendCollection = new BlockingCollection<ITransportMessage>();
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            const string senderIdentity = "RequestorIdentity";
            const string sentMessageType = "Type";
            const string sentData = "Data";
            _socketManager.CreateResponseSocket(receiveCollection, sendCollection, Endpoint, "ReplierIdentity");
            var requestorSocket = CreateRequestorSocket(senderIdentity, Endpoint);

            requestorSocket.SendMore(Encoding.ASCII.GetBytes(sentMessageType));
            requestorSocket.Send(Encoding.ASCII.GetBytes(sentData));

            var receivedMessage = receiveCollection.Take();
            Assert.AreEqual(senderIdentity, receivedMessage.SenderIdentity);
            Assert.AreEqual(sentMessageType, receivedMessage.MessageType);
            Assert.AreEqual(sentData, Encoding.ASCII.GetString(receivedMessage.Data));

            const string ackMessageType = "Ack";
            const string ackMessageData = "Reply";
            var ackMessage = new TransportMessage(senderIdentity, ackMessageType, Encoding.ASCII.GetBytes(ackMessageData));
            sendCollection.Add(ackMessage);

            Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(ackMessage.MessageType, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(ackMessage.Data, requestorSocket.Receive());

            requestorSocket.Dispose();

        }

        private ZmqSocket CreateRequestorSocket(string senderIdentity, string endpoint)
        {
            var requestorSocket = _zmqContext.CreateSocket(SocketType.DEALER);
            requestorSocket.Identity = Encoding.ASCII.GetBytes(senderIdentity);
            requestorSocket.Connect(endpoint);
            return requestorSocket;
        }


        [TearDown]
        public void TearDown()
        {
            _socketManager.Stop();
            _zmqContext.Dispose();
        }
    }
}
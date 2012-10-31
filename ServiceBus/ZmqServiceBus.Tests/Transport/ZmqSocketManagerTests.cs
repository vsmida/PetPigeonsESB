﻿using System;
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
        private byte[] _ephemeralSocketId;

        [SetUp]
        public void Setup()
        {
            _zmqContext = ZmqContext.Create();
            _socketManager = new ZmqSocketManager(_zmqContext);
        }

        [Test, Timeout(10000000), Repeat(10)]
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
            _socketManager.CreateRequestSocket(sendCollection, receiveCollection, Endpoint);
            var messageIdentity = Guid.NewGuid();
            sendCollection.Add(new TransportMessage(messageIdentity, null, commandType, Encoding.ASCII.GetBytes(commandData)));

            var ephemeralIdentity = routerSocket.Receive();//identity of client
            Assert.AreEqual(messageIdentity, new Guid(routerSocket.Receive()));
            Assert.AreEqual(commandType, Encoding.ASCII.GetString(routerSocket.Receive()));
            Assert.AreEqual(commandData, Encoding.ASCII.GetString(routerSocket.Receive()));

            routerSocket.SendMore(ephemeralIdentity);
            routerSocket.SendMore(new byte[0]);
            //routerSocket.SendMore("RouterServiceId", Encoding.ASCII);
            routerSocket.SendMore(messageIdentity.ToByteArray());
            routerSocket.SendMore(Encoding.ASCII.GetBytes(ackType));
            routerSocket.Send(Encoding.ASCII.GetBytes(ackData));

            var receivedAck = receiveCollection.Take();
            Assert.AreEqual(ackType, receivedAck.MessageType);
            Assert.AreEqual(Encoding.ASCII.GetBytes(ackData), receivedAck.Data);
            Assert.AreEqual(messageIdentity, receivedAck.MessageIdentity);

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

            var messageIdentity = Guid.NewGuid();
            var transportMessage = new TransportMessage(messageIdentity,null, messageType, Encoding.ASCII.GetBytes(messageData));
            sendQueue.Add(transportMessage);

            Assert.AreEqual(Encoding.ASCII.GetBytes(transportMessage.MessageType), subscriberSocket.Receive());
            Assert.AreEqual(messageIdentity.ToByteArray(), subscriberSocket.Receive());
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
            var messageId = Guid.NewGuid();
            publisherSocket.SendMore(messageId.ToByteArray());
            publisherSocket.SendMore(Encoding.ASCII.GetBytes(messageType));
            publisherSocket.Send(Encoding.ASCII.GetBytes(messageData));

            var receivedMessage = receiveCollection.Take();
            Assert.AreEqual(messageType, receivedMessage.MessageType);
            Assert.AreEqual(messageId, receivedMessage.MessageIdentity);
            Assert.AreEqual(Encoding.ASCII.GetBytes(messageData), receivedMessage.Data);

            publisherSocket.Dispose();
        }

        [Test, Timeout(1000), Repeat(10)]
        public void should_send_back_ack_when_message_received_on_response_socket()
        {
            var sendCollection = new BlockingCollection<ITransportMessage>();
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            const string senderIdentity = "RequestorIdentity";
            const string sentMessageType = "Type";
            const string sentData = "Data";
            var messageId = Guid.NewGuid();
            _socketManager.CreateResponseSocket(receiveCollection, sendCollection, Endpoint);
            var requestorSocket = CreateRequestorSocket(Endpoint);

            requestorSocket.SendMore(messageId.ToByteArray());
            requestorSocket.SendMore(Encoding.ASCII.GetBytes(sentMessageType));
            requestorSocket.Send(Encoding.ASCII.GetBytes(sentData));

            var receivedMessage = receiveCollection.Take();
//            Assert.AreEqual(senderIdentity, receivedMessage.SenderIdentity);
            Assert.AreEqual(sentMessageType, receivedMessage.MessageType);
            Assert.AreEqual(messageId, receivedMessage.MessageIdentity);
            Assert.AreEqual(sentData, Encoding.ASCII.GetString(receivedMessage.Data));

            Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(messageId.ToByteArray(), requestorSocket.Receive());
            Assert.AreEqual(typeof(ReceivedOnTransportAcknowledgement).FullName, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));

            requestorSocket.Dispose();

        }

        [Test, Timeout(1000), Repeat(1)]
        public void should_not_send_back_ack_when_message_received_on_response_socket_is_already_a_transport_ack()
        {
            var sendCollection = new BlockingCollection<ITransportMessage>();
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            const string senderIdentity = "RequestorIdentity";
            const string sentMessageType = "Type";
            const string sentData = "Data";
            var messageId = Guid.NewGuid();
            _socketManager.CreateResponseSocket(receiveCollection, sendCollection, Endpoint);
            var requestorSocket = CreateRequestorSocket(Endpoint);

            requestorSocket.SendMore(messageId.ToByteArray());
            requestorSocket.SendMore(Encoding.ASCII.GetBytes(typeof(ReceivedOnTransportAcknowledgement).FullName));
            requestorSocket.Send(new byte[0]);

            Assert.AreEqual(null, requestorSocket.Receive(Encoding.ASCII, TimeSpan.FromMilliseconds(100)));
            Assert.AreEqual(null, requestorSocket.Receive(Encoding.ASCII, TimeSpan.FromMilliseconds(100)));
            //Assert.AreEqual(messageId.ToByteArray(), requestorSocket.Receive());
            //Assert.AreEqual(typeof(ReceivedOnTransportAcknowledgement).FullName, requestorSocket.Receive(Encoding.ASCII));
            //Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));

            requestorSocket.Dispose();

        }



        [Test, Timeout(1000000), Repeat(10)]
        public void should_receive_messages_on_replier_socket_and_route_messages_back()
        {
            var sendCollection = new BlockingCollection<ITransportMessage>();
            var receiveCollection = new BlockingCollection<ITransportMessage>();
            const string sentMessageType = "Type";
            const string sentData = "Data";
            var messageId = Guid.NewGuid();
            _socketManager.CreateResponseSocket(receiveCollection, sendCollection, Endpoint);
            var requestorSocket = CreateRequestorSocket(Endpoint);

            requestorSocket.SendMore(messageId.ToByteArray());
            requestorSocket.SendMore(Encoding.ASCII.GetBytes(sentMessageType));
            requestorSocket.Send(Encoding.ASCII.GetBytes(sentData));

            var receivedMessage = receiveCollection.Take();
            var requestorSocketId = receivedMessage.SendingSocketId;
           // Assert.AreEqual(senderIdentity, receivedMessage.SenderIdentity);
            Assert.AreEqual(sentMessageType, receivedMessage.MessageType);
            Assert.AreEqual(messageId, receivedMessage.MessageIdentity);
            Assert.AreEqual(sentData, Encoding.ASCII.GetString(receivedMessage.Data));

            //ack
            Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(messageId.ToByteArray(), requestorSocket.Receive());
            Assert.AreEqual(typeof(ReceivedOnTransportAcknowledgement).FullName, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));

            //sending mess
            const string ackMessageType = "Ack";
            const string ackMessageData = "Reply";
            var ackMessage = new TransportMessage(messageId, requestorSocketId, ackMessageType, Encoding.ASCII.GetBytes(ackMessageData));
            sendCollection.Add(ackMessage);

            Assert.AreEqual(string.Empty, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(ackMessage.MessageIdentity, new Guid(requestorSocket.Receive()));
            Assert.AreEqual(ackMessage.MessageType, requestorSocket.Receive(Encoding.ASCII));
            Assert.AreEqual(ackMessage.Data, requestorSocket.Receive());

            requestorSocket.Dispose();

        }

        private ZmqSocket CreateRequestorSocket(string endpoint)
        {
            var requestorSocket = _zmqContext.CreateSocket(SocketType.DEALER);
      //      requestorSocket.Identity = Encoding.ASCII.GetBytes(senderIdentity);
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
using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Shared;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class SubscriptionManagerTests
    {
        private class FakeMessage : IEvent
        {
            
        }

        private SubscriptionManager _subscriptionManager;
        private Mock<IPeerManager> _peerManagerMock;

        [SetUp]
        public void setup()
        {
            _peerManagerMock = new Mock<IPeerManager>();
            _subscriptionManager = new SubscriptionManager(_peerManagerMock.Object);
        }

        [Test]
        public void should_raise_subscription_event_when_someone_subscribes()
        {
            Type eventType = null;
            _subscriptionManager.NewEventSubscription += x => eventType = x;
            
            _subscriptionManager.StartListeningTo<FakeMessage>();

            Assert.AreEqual(typeof(FakeMessage), eventType);
        }

        [Test]
        public void should_raise_unsubscribe_event_when_subscription_object_disposed()
        {
            Type eventType = null;
            _subscriptionManager.EventUnsubscibe += x => eventType = x;

            var subscription = _subscriptionManager.StartListeningTo<FakeMessage>();

            Assert.IsNull(eventType);

            subscription.Dispose();

            Assert.AreEqual(typeof(FakeMessage), eventType);
        }

        //[Test]
        //public void should_raise_subscribe_event_when_relevant_peer_publisher_connects()
        //{
        //    _subscriptionManager.StartListeningTo<FakeMessage>();
        //    Type eventType = null;
        //    _subscriptionManager.NewEventSubscription += x => eventType = x;

        //    _peerManagerMock.Raise(x => x.PeerConnected += y =>{}, new ServicePeer("toto", "endpoint","end2", new List<Type>() ));

        //    Assert.AreEqual(typeof(FakeMessage), eventType);
        //}
    }
}
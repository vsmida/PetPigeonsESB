using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class PeerManagerTests
    {

        private class FakeCommand : ICommand
        {
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

        }

        private class FakeEvent : IEvent
        {
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

        }

        private PeerManager _peerManager;
        [SetUp]
        public void setup()
        {
            //_peerManager = new PeerManager();
        }

        //[Test]
        //public void should_register_peers_and_provide_endpoints()
        //{
        //    var peer = GetPeer();

        //    _peerManager.RegisterPeer(peer);

        //    Assert.AreEqual(peer.ReceptionEndpoint, _peerManager.GetEndpointsForMessageType(typeof(FakeCommand).FullName).Single());
        //    Assert.AreEqual(peer.PublicationEndpoint, _peerManager.GetEndpointsForMessageType(typeof(FakeEvent).FullName).Single());
        //}


        [Test]
        public void should_register_peers_and_raise_event()
        {
            var peer = GetPeer();
            ServicePeer raisedPeer = null;
            _peerManager.PeerConnected += x => raisedPeer = x;
            
            _peerManager.RegisterPeerConnection(peer);

            Assert.AreEqual(raisedPeer, peer);
        }

        [Test]
        public void should_give_right_endpoint_when_routing()
        {
            var peer = GetPeer();
            var peer2 = GetPeer("Test2", "T12");

            _peerManager.RegisterPeerConnection(peer);
            _peerManager.RegisterPeerConnection(peer2);

            Assert.AreEqual(peer2.HandledMessages.Single(), _peerManager.GetPeerSubscriptionFor(typeof(FakeCommand).FullName, peer2.PeerName));
        }

        private static ServicePeer GetPeer(string peerName = null, string receptionEdnpoint = null)
        {
            return null;
            // return new ServicePeer(peerName ?? "Test",receptionEdnpoint ?? "T1", "T2", new List<Type> { typeof(FakeCommand) });
        }
    }
}
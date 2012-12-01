﻿using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Shared;
using ZmqServiceBus.Bus.Handlers;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Tests.Transport;

namespace ZmqServiceBus.Tests.InfrastructureHandlers
{
    public class DirectoryServiceMessagesHandlerTests
    {
        private DirectoryServiceMessagesHandler _handler;
        private Mock<IPeerManager> _peerManagerMock;
        private Mock<IMessageOptionsRepository> _optionsRepoMock;


        [SetUp]
        public void setup()
        {
            _peerManagerMock = new Mock<IPeerManager>();
            _optionsRepoMock = new Mock<IMessageOptionsRepository>();
            _handler = new DirectoryServiceMessagesHandler(_peerManagerMock.Object, _optionsRepoMock.Object);
        }

        [Test]
        public void should_register_connected_peer_with_peerManager()
        {
            var servicePeer = TestData.GenerateServicePeer();
            var peerConnected = new PeerConnected(servicePeer);

            _handler.Handle(peerConnected);

            _peerManagerMock.Verify(x => x.RegisterPeer(servicePeer));
        }

        [Test]
        public void should_initialize_topology_and_messages()
        {
            var peer = TestData.GenerateServicePeer();
            var option = new MessageOptions("type", new ReliabilityInfo(ReliabilityLevel.FireAndForget));
            var command = new InitializeTopologyAndMessageSettings(new List<ServicePeer> { (ServicePeer)peer },
                                                                    new List<MessageOptions> {option});

            _handler.Handle(command);

            _peerManagerMock.Verify(x => x.RegisterPeer(peer));
            _optionsRepoMock.Verify(x => x.RegisterOptions(option));
        }
    }
}
﻿using System;
using Shared;
using Bus;
using Bus.MessageInterfaces;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;

namespace Tests.Transport
{
    public static class TestData
    {
        public class FakeCommand : ICommand
        {
        }

        public class FakeEvent
        { }

        public class FakeCommandHandler : ICommandHandler<FakeCommand>
        {
            public static event Action<FakeCommand> HandlingCommand = delegate { };

            public void Handle(FakeCommand item)
            {
                HandlingCommand(item);
            }
        }

        public class CommandThatThrows : ICommand
        {
        }

        public class CommandThatThrowsHandler : ICommandHandler<CommandThatThrows>
        {
            public void Handle(CommandThatThrows item)
            {
                throw new Exception("throwing");
            }
        }

        public static ServicePeer GenerateServicePeer()
        {
            return null;
            //return new ServicePeer("peerName", "reception", "publication", new List<Type> { typeof(FakeCommand) });
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(Guid? id = null)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", id ?? Guid.NewGuid(), WireTransportType.ZmqPushPullTransport, new byte[0]);
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(T item)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", Guid.NewGuid(), WireTransportType.ZmqPushPullTransport, BusSerializer.Serialize(item));
        }

        public static ServicePeer CreatePeerThatHandles<T>(string receptionEndpoint, string peerName = null)
        {
            return null;
            // return new ServicePeer(peerName ?? "Name", receptionEndpoint, "", new List<Type> { typeof(T) });
        }

        public static ServicePeer CreatePeerThatPublishes<T>(string pubEndpoint)
        {
            return null;
            //  return new ServicePeer("Name", "", pubEndpoint, new List<Type>());
        }
    }
}
using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Tests.Transport
{
    public static class TestData
    {
        public class FakeCommand : ICommand
        {
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

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
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }

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

        public static SendingBusMessage GenerateDummySendingMessage<T>()
        {
            return new SendingBusMessage(typeof(T).FullName, Guid.NewGuid(), new byte[0], null);
        }

        public static SendingBusMessage GenerateDummySendingMessage<T>(T item)
        {
            return new SendingBusMessage(typeof(T).FullName, Guid.NewGuid(), BusSerializer.Serialize(item), null);
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
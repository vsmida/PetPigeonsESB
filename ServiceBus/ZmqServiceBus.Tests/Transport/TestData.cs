using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests.Transport
{
    public static class TestData
    {
        public class FakeCommand : ICommand
        { }

        public class FakeEvent
        { }

        public class FakeCommandHandler : ICommandHandler<FakeCommand>
        {
            public static event Action<FakeCommand> HandlingCommand = delegate {};

            public void Handle(FakeCommand item)
            {
                HandlingCommand(item);
            }
        }

        public class CommandThatThrows : ICommand
        {
            
        }

        public class CommandThatThrowsHandler:ICommandHandler<CommandThatThrows>

        {
            public void Handle(CommandThatThrows item)
            {
                    throw new Exception("throwing");
            }
        }

        public static IServicePeer GenerateServicePeer()
        {
            return new ServicePeer("peerName", "reception", "publication", new List<Type> { typeof(FakeCommand) }, new List<Type> { typeof(FakeEvent) });
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(Guid? id = null)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", id ?? Guid.NewGuid(), new byte[0]);
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(T item)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", Guid.NewGuid(), Serializer.Serialize(item));
        }

        public static SendingBusMessage GenerateDummySendingMessage<T>()
        {
            return new SendingBusMessage(typeof(T).FullName, Guid.NewGuid(), new byte[0]);
        }

        public static SendingBusMessage GenerateDummySendingMessage<T>(T item)
        {
            return new SendingBusMessage(typeof(T).FullName, Guid.NewGuid(), Serializer.Serialize(item));
        }

        public static IServicePeer CreatePeerThatHandles<T>(string receptionEndpoint, string peerName = null)
        {
            return new ServicePeer(peerName ?? "Name", receptionEndpoint, "", new List<Type> { typeof(T) }, new List<Type>());
        }

        public static IServicePeer CreatePeerThatPublishes<T>(string pubEndpoint)
        {
            return new ServicePeer("Name", "", pubEndpoint, new List<Type>(), new List<Type> { typeof(T) });
        }
    }
}
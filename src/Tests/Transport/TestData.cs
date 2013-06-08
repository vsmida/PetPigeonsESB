using System;
using System.IO;
using Bus.Attributes;
using Bus.Serializer;
using ProtoBuf;
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
        [ProtoContract]
        public class FakeCommand : ICommand
        {
        }

        public class FakeEndpointType : IEndpoint
        {
            public bool Equals(IEndpoint other)
            {
                return true;
            }

            public WireTransportType WireTransportType { get; private set; }
            public bool IsMulticast { get; private set; }
            public Stream Serialize()
            {
                return new MemoryStream();
            }

            public IEndpoint Deserialize(Stream stream)
            {
                return new FakeEndpointType();
            }
        }

        public class FakeEndpointTypeSerializer: EndpointSerializer<FakeEndpointType>
        {
            public override Stream Serialize(FakeEndpointType item)
            {
                return new MemoryStream();
            }

            public override FakeEndpointType Deserialize(Stream item)
            {
                return new FakeEndpointType();
            }
        }

        public class FakeEvent
        { }

        [StatelessHandler]
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


        public class FakeCommandSerializer : BusMessageSerializer<FakeCommand>
        {
            public static event Action<FakeCommand> SerializeCalled = delegate { };
            public static event Action<byte[]> DeserializeCalled = delegate { };
            public override FakeCommand Deserialize(byte[] serializedMessage)
            {
                DeserializeCalled(serializedMessage);
                return new FakeCommand();
            }

            public override byte[] Serialize(FakeCommand item)
            {
                SerializeCalled(item);
                return new byte[0];
            }
        }

        public static ServicePeer GenerateServicePeer()
        {
            return null;
            //return new ServicePeer("peerName", "reception", "publication", new List<Type> { typeof(FakeCommand) });
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(Guid? id = null, int sequenceNumber = 0, string peer = "Peer")
        {
            return new ReceivedTransportMessage(typeof(T).FullName, peer, id ?? Guid.NewGuid(), null, new byte[0], sequenceNumber);
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(T item)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", Guid.NewGuid(), null, BusSerializer.Serialize(item), 0);
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
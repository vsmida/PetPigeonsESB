using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Tests.Transport
{
    public static class TestData
    {
        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(Guid? id = null)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", id ?? Guid.NewGuid(), new byte[0]);
        }

        public static ReceivedTransportMessage GenerateDummyReceivedMessage<T>(T item)
        {
            return new ReceivedTransportMessage(typeof(T).FullName, "Peer", Guid.NewGuid(), Serializer.Serialize(item));
        }

        public static SendingTransportMessage GenerateDummySendingMessage<T>()
        {
            return new SendingTransportMessage(typeof(T).FullName, Guid.NewGuid(), new byte[0]);
        }

        public static SendingTransportMessage GenerateDummySendingMessage<T>(T item)
        {
            return new SendingTransportMessage(typeof(T).FullName, Guid.NewGuid(), Serializer.Serialize(item));
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
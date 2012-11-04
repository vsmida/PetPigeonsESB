using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Tests.Transport
{
    public static class TestData
    {
         public static TransportMessage GenerateDummyMessage<T>()
         {
             return new TransportMessage(Guid.NewGuid(), new byte[0],typeof(T).FullName, new byte[0]);
         }

         public static TransportMessage GenerateDummyMessage<T>(T item)
         {
             return new TransportMessage(Guid.NewGuid(), new byte[0], typeof(T).FullName, Serializer.Serialize(item));
         }

        public static IServicePeer CreatePeerThatHandles<T>(string receptionEndpoint)
        {
            return new ServicePeer("Name", receptionEndpoint, "", new List<Type>{typeof(T)},new List<Type>());
        }

        public static IServicePeer CreatePeerThatPublishes<T>(string pubEndpoint)
        {
            return new ServicePeer("Name", "", pubEndpoint, new List<Type> (), new List<Type>{typeof(T)});
        }
    }
}
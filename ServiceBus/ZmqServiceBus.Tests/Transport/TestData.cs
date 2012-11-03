using System;
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
    }
}
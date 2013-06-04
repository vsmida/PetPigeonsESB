using System;
using System.IO;
using Bus.MessageInterfaces;

namespace Bus.Serializer
{
    internal interface IMessageSerializer
    {
        byte[] Serialize(IMessage item);
        IMessage Deserialize(byte[] serializedMessage);

    }

    public abstract class BusMessageSerializer<T> : IMessageSerializer where T: IMessage
    {
        public byte[] Serialize(IMessage item)
        {
            return Serialize((T)item);
        }

        IMessage IMessageSerializer.Deserialize(byte[] serializedMessage)
        {
            return Deserialize(serializedMessage);
        }

        public abstract byte[] Serialize(T item);
        public abstract T Deserialize(byte[] item);
    }
}
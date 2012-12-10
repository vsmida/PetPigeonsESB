using System.IO;
using ProtoBuf;
using ZeroMQ;

namespace Shared
{
    public static class ExtendSocket
    {
        public static byte[] Receive(this ZmqSocket socket)
        {
            int size;
            return socket.Receive(null, out size);
        }

        public static T DeserializeAndReceive<T>(this ZmqSocket socket) where T:class
        {
            return Serializer.Deserialize<T>(socket.Receive());
        }


    }
}
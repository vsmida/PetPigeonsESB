using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using Bus.Subscriptions;
using Bus.Transport.Network;
using ProtoBuf.Meta;

namespace Bus
{
    public static class BusSerializer
    {
        private static readonly RuntimeTypeModel _model;
        private static ThreadLocal<MemoryStream> _memoryStream = new ThreadLocal<MemoryStream>(() => new MemoryStream());

        static BusSerializer()
        {
            _model = RuntimeTypeModel.Default;

            _model.Add(typeof(IEndpoint), false).AddSubType(1, typeof(ZmqEndpoint));
            _model.Add(typeof(ISubscriptionFilter), false).AddSubType(1, typeof(DummySubscriptionFilter));
            _model.Add(typeof(ISubscriptionFilter), false).AddSubType(2, typeof(SynchronizeWithBrokerFilter));
            _model.AutoCompile = true;
            _model.CompileInPlace();
            
        }

        public static byte[] Serialize(object instance)
        {
            var memoryStream = _memoryStream.Value;
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.SetLength(0);

            _model.Serialize(memoryStream, instance);
            return memoryStream.ToArray();

        }

        public static ArraySegment<byte> SerializeAndGetRawBuffer(object instance)
        {
            var memoryStream = _memoryStream.Value;
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.SetLength(0);

            _model.Serialize(memoryStream, instance);
            return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

        }

        public static T DeserializeStruct<T>(byte[] data) where T : struct
        {
            using (var stream = new MemoryStream(data))
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
            }
        }


        public static T Deserialize<T>(byte[] data) where T : class
        {
            return (T)Deserialize(data, typeof(T));
        }


        public static object Deserialize(ArraySegment<byte> data, Type type)
        {

            _memoryStream.Value.Seek(0, SeekOrigin.Begin);
            _memoryStream.Value.Write(data.Array, data.Offset, data.Count);
            _memoryStream.Value.Seek(0, SeekOrigin.Begin);

            _memoryStream.Value.SetLength(data.Count);

            var obj = FormatterServices.GetUninitializedObject(type);
            _model.Deserialize(_memoryStream.Value, obj, type);
            return obj;

        }


        public static object Deserialize(byte[] data, Type type)
        {
            var memoryStream = _memoryStream.Value;
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream.SetLength(data.Length);

            var obj = FormatterServices.GetUninitializedObject(type);
            _model.Deserialize(memoryStream, obj, type);
            return obj;

        }
    }
}
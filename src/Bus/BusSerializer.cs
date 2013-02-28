using System;
using System.IO;
using System.Runtime.Serialization;
using Bus.Subscriptions;
using Bus.Transport.Network;
using ProtoBuf.Meta;

namespace Bus
{
    public static class BusSerializer
    {
        private static readonly RuntimeTypeModel _model;
        [ThreadStatic]
        private static MemoryStream _memoryStream;

        static BusSerializer()
        {
            _model = RuntimeTypeModel.Default;

            _model.Add(typeof(IEndpoint), false).AddSubType(1, typeof(ZmqEndpoint));
            _model.Add(typeof(ISubscriptionFilter), false).AddSubType(1, typeof(DummySubscriptionFilter));
            _model.Add(typeof(ISubscriptionFilter), false).AddSubType(2, typeof(SynchronizeWithBrokerFilter));
            _model.AutoCompile = true;
            _model.CompileInPlace();
        }

        public static ArraySegment<byte> Serialize(object instance)
        {
            if (_memoryStream == null)
                _memoryStream = new MemoryStream();

            _memoryStream.Seek(0, SeekOrigin.Begin);
            _memoryStream.SetLength(0);

            _model.Serialize(_memoryStream, instance);
            return new ArraySegment<byte>(_memoryStream.ToArray(),0,(int)_memoryStream.Length);

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
            if (_memoryStream == null)
                _memoryStream = new MemoryStream();

            _memoryStream.Seek(0, SeekOrigin.Begin);
            _memoryStream.Write(data.Array, data.Offset, data.Count);
            _memoryStream.Seek(0, SeekOrigin.Begin);

            _memoryStream.SetLength(data.Count);

            var obj = FormatterServices.GetUninitializedObject(type);
            _model.Deserialize(_memoryStream, obj, type);
            return obj;

        }


        public static object Deserialize(byte[] data, Type type)
        {
            if (_memoryStream == null)
                _memoryStream = new MemoryStream();

            _memoryStream.Seek(0, SeekOrigin.Begin);
            _memoryStream.Write(data, 0, data.Length);
            _memoryStream.Seek(0, SeekOrigin.Begin);

            _memoryStream.SetLength(data.Length);

            var obj = FormatterServices.GetUninitializedObject(type);
            _model.Deserialize(_memoryStream, obj, type);
            return obj;

        }
    }
}
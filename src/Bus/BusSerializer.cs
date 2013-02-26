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
        private static RuntimeTypeModel _model;

        static BusSerializer()
        {
            _model = RuntimeTypeModel.Default;

            _model.Add(typeof (IEndpoint), false).AddSubType(1, typeof (ZmqEndpoint));
            _model.Add(typeof(ISubscriptionFilter), false).AddSubType(1, typeof(DummySubscriptionFilter));
            _model.Add(typeof(ISubscriptionFilter), false).AddSubType(2, typeof(SynchronizeWithBrokerFilter));
            _model.AutoCompile = true;
            _model.CompileInPlace();
        }

        public static byte[] Serialize(object instance)
        {
            using (var stream = new MemoryStream())
            {
                _model.Serialize(stream, instance);
                return stream.ToArray();
            }
        }

        public static T DeserializeStruct<T>(byte[] data) where T : struct
        {
            using (var stream = new MemoryStream(data))
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
            }
        }


        public static T Deserialize<T>(byte[] data) where T:class 
        {
            using (var stream = new MemoryStream(data))
            {
                var obj = FormatterServices.GetUninitializedObject(typeof(T));
                _model.Deserialize(stream, obj, typeof(T));
                return (T)obj;
            }
        }

        public static object Deserialize(byte[] data, Type type)
        {
            using (var stream = new MemoryStream(data))
            {
                var obj = FormatterServices.GetUninitializedObject(type);
                _model.Deserialize(stream, obj, type);
                return obj;
            }
        }
    }
}
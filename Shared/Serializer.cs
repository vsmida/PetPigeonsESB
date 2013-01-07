using System;
using System.IO;
using System.Runtime.Serialization;
using ProtoBuf.Meta;

namespace Shared
{
    internal static class Serializer
    {

        public static byte[] Serialize(object instance)
        {
            using (var stream = new MemoryStream())
            {
                RuntimeTypeModel.Default.Serialize(stream, instance);
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
                RuntimeTypeModel.Default.Deserialize(stream, obj, typeof(T));
                return (T)obj;
            }
        }

        public static object Deserialize(byte[] data, Type type)
        {
            using (var stream = new MemoryStream(data))
            {
                var obj = FormatterServices.GetUninitializedObject(type);
                RuntimeTypeModel.Default.Deserialize(stream, obj, type);
                return obj;
            }
        }
    }
}
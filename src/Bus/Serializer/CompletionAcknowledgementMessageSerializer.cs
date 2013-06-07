using System;
using System.Collections.Generic;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.Transport.Network;
using Shared;

namespace Bus.Serializer
{
    public class CompletionAcknowledgementMessageSerializer : BusMessageSerializer<CompletionAcknowledgementMessage>
    {
        private Dictionary<Type, IEndpointSerializer> _endpointSerializersByType = new Dictionary<Type, IEndpointSerializer>();

        public CompletionAcknowledgementMessageSerializer(IAssemblyScanner scanner)
        {
            foreach (var pair in scanner.FindEndpointTypesToSerializers())
            {
                _endpointSerializersByType.Add(pair.Key, Activator.CreateInstance(pair.Value) as IEndpointSerializer);
            }
        }

        public override byte[] Serialize(CompletionAcknowledgementMessage item)
        {
            var serializedEndpoint = SerializeEndpoint(item.Endpoint);
            var length = 4 + serializedEndpoint.Length + 16 + 4 + item.MessageType.Length + 1;
            var result = new byte[length];
            ByteUtils.WriteInt(result, 0, length - 4);
            for (int i = 0; i < serializedEndpoint.Length; i++)
            {
                result[i + 4] = serializedEndpoint[i];
            }
            var guidBytes = item.MessageId.ToByteArray();
            Array.Copy(guidBytes,0, result, 4 + serializedEndpoint.Length, 16);
            ByteUtils.WriteInt(result, 4 + serializedEndpoint.Length + 16, item.MessageType.Length);
            for (int i = 0; i < item.MessageType.Length; i++)
            {
                result[4 + serializedEndpoint.Length + 16 + 4 + i] = (byte)item.MessageType[i];
            }
            result[result.Length - 1] = Convert.ToByte(item.ProcessingSuccessful);
            return result;
        }
        public override CompletionAcknowledgementMessage Deserialize(byte[] item)
        {
            var totalLength = ByteUtils.ReadInt(item, 0);
            var endpointLength = ByteUtils.ReadInt(item, 4);
            var endpointTypeLength = ByteUtils.ReadInt(item, 8);
            var endpointType = ByteUtils.ReadAsciiStringFromArray(item, 12, endpointTypeLength);
            IEndpoint endpoint = null;
            if(endpointType == typeof(ZmqEndpoint).FullName)
            {
                var adressString = ByteUtils.ReadAsciiStringFromArray(item, 12 + endpointTypeLength,
                                                                      endpointLength - 4 - endpointTypeLength);
                endpoint = new ZmqEndpoint(adressString);
            }

            var idArray = new byte[16];
            Array.Copy(item, 4 + 4 + endpointLength,idArray,0,16);
            var messageId = new Guid(idArray);

            var messageTypeLength = ByteUtils.ReadInt(item, 4 + 4 + endpointLength + 16);
            var messageType = ByteUtils.ReadAsciiStringFromArray(item, 4 + 4 + endpointLength + 16 + 4,
                                                                 messageTypeLength);
            var success = Convert.ToBoolean(item[item.Length - 1]);

            return new CompletionAcknowledgementMessage(messageId, messageType, success, endpoint);
        }

        private byte[] SerializeEndpoint(IEndpoint endpoint)
        {
            if(endpoint is ZmqEndpoint)
            {
                var type = typeof (ZmqEndpoint).FullName;
                var zmqEndpoint = (ZmqEndpoint) endpoint;
                var totalLength = 4 + 4 + type.Length + zmqEndpoint.Endpoint.Length;
                var result = new byte[totalLength];
                ByteUtils.WriteInt(result, 0, totalLength-4);
                ByteUtils.WriteInt(result, 4, type .Length);
                for (int i = 0; i < type.Length; i++)
                {
                    result[i + 8] = (byte) type[i];
                }
                for (int i = 0; i < zmqEndpoint.Endpoint.Length; i++)
                {
                    result[i + 8 + type.Length] = (byte) zmqEndpoint.Endpoint[i];
                }
                return result;
            }
            throw new ArgumentException("unexpected endpoint type");
        }


    }
}
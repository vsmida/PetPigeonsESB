using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Bus.Dispatch;
using Bus.Transport.SendingPipe;
using Shared;

namespace Bus.Serializer
{
    public class MessageWireDataSerializer
    {
        private readonly Dictionary<string, int> _messageTypeToId = new Dictionary<string, int>();
        private readonly Dictionary<int, string> _messageTypeIdToMessageType = new Dictionary<int, string>();
        public MessageWireDataSerializer(IAssemblyScanner assemblyScanner)
        {
            var knownMessages = assemblyScanner.GetMessageOptions();
            foreach (var messageOptionse in knownMessages)
            {
                try
                {
                    var fullName = messageOptionse.MessageType.FullName;
                    var idFromString = StringUtils.CreateIdFromString(fullName);
                    _messageTypeToId.Add(fullName, idFromString);
                    _messageTypeIdToMessageType.Add(idFromString, fullName);

                }
                catch(ArgumentException ex)
                {
                    throw new ArgumentException("Problem while loading message type to message type id dictionary, two type names might have the same id");
                }
            }
        }

        public byte[] Serialize(MessageWireData data)
        {
            var sendingPeerArray = data.SendingPeer.ToCharArray();
            var guidLength = 16;
            var length = 4 + data.Data.Length + guidLength + 4 + 4 + 4 + sendingPeerArray.Length +
                         (data.SequenceNumber == null ? 0 : 4);
            var finalArray = new byte[length];

            var idByteArray = data.MessageIdentity.ToByteArray();
            idByteArray.CopyTo(finalArray, 0);

            ByteUtils.WriteInt(finalArray, guidLength, _messageTypeToId[data.MessageType]);
            ByteUtils.WriteInt(finalArray, guidLength + 4, sendingPeerArray.Length);
            var sendingPeerOffset = guidLength + 4 + 4;
            for (int i = 0; i < data.SendingPeer.Length; i++)
            {
                finalArray[sendingPeerOffset + i] = (byte)data.SendingPeer[i];
            }

            var dataOffset = sendingPeerOffset + sendingPeerArray.Length;
            ByteUtils.WriteInt(finalArray, dataOffset, data.Data.Length);
            data.Data.CopyTo(finalArray, dataOffset + 4);

            if (data.SequenceNumber != null)
                ByteUtils.WriteInt(finalArray, dataOffset + 4 + data.Data.Length, data.SequenceNumber.Value);

            return finalArray;
        }

        public MessageWireData Deserialize(Stream data)
        {
            var idArray = new byte[16];
            data.Read(idArray, 0, 16);
            var id = new Guid(idArray);
            var messageTypeId = ByteUtils.ReadIntFromStream(data);
            var sendingPeerLength = ByteUtils.ReadIntFromStream(data);
            var sendingPeer = ByteUtils.ReadAsciiStringFromStream(data, sendingPeerLength);
            var dataLength = ByteUtils.ReadIntFromStream(data);
            var binaryData = new byte[dataLength];
            data.Read(binaryData, 0, dataLength);
            int? sequenceNumber = null;
            if (data.Length > 16 + 4 + 4 + 4 + sendingPeerLength + 4 + dataLength)
                sequenceNumber = ByteUtils.ReadIntFromStream(data);

            return new MessageWireData(_messageTypeIdToMessageType[messageTypeId], id, sendingPeer, binaryData) { SequenceNumber = sequenceNumber };

        }
    }
}
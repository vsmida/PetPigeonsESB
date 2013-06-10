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
        private readonly ISerializationHelper _serializationHelper;
        public MessageWireDataSerializer(ISerializationHelper serializationHelper)
        {
            _serializationHelper = serializationHelper;
        }

        public byte[] Serialize(MessageWireData data)
        {
            var guidLength = 16;
            var length = 4 + data.Data.Length + guidLength + 4 + 4 + 4 + 4;
            var finalArray = new byte[length];

            var idByteArray = data.MessageIdentity.ToByteArray();
            idByteArray.CopyTo(finalArray, 0);

            ByteUtils.WriteInt(finalArray, guidLength, _serializationHelper.GetMessageTypeId(data.MessageType));
            ByteUtils.WriteInt(finalArray, guidLength + 4, data.SendingPeerId.Id);
            var sendingPeerOffset = guidLength + 4 + 4;

            ByteUtils.WriteInt(finalArray, sendingPeerOffset, data.Data.Length);
            data.Data.CopyTo(finalArray, sendingPeerOffset + 4);

            if (data.SequenceNumber != null)
                ByteUtils.WriteInt(finalArray, sendingPeerOffset + 4 + data.Data.Length, data.SequenceNumber.Value);
            else
            {
                ByteUtils.WriteInt(finalArray, sendingPeerOffset + 4 + data.Data.Length, -1);
            }

            return finalArray;
        }

        public MessageWireData Deserialize(Stream data)
        {
            var idArray = new byte[16];
            data.Read(idArray, 0, 16);
            var id = new Guid(idArray);
            var messageTypeId = ByteUtils.ReadIntFromStream(data);
            var sendingPeer = ByteUtils.ReadIntFromStream(data);
            var dataLength = ByteUtils.ReadIntFromStream(data);
            if (dataLength == -1)
             dataLength = ByteUtils.ReadIntFromStream(data);

            var binaryData = new byte[dataLength];
            data.Read(binaryData, 0, dataLength);
            int? sequenceNumber = null;
                sequenceNumber = ByteUtils.ReadIntFromStream(data);

            return new MessageWireData(_serializationHelper.GetMessageTypeFromId(messageTypeId), id, new PeerId(sendingPeer), binaryData) { SequenceNumber = sequenceNumber == -1 ? null : sequenceNumber };

        }
    }
}
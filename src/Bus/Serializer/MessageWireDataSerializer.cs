using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Bus.Transport.SendingPipe;
using Shared;

namespace Bus.Serializer
{
    public class MessageWireDataSerializer
    {
        public byte[] Serialize(MessageWireData data)
        {
            var messageTypeArray = data.MessageType.ToCharArray();
            var sendingPeerArray = data.SendingPeer.ToCharArray();
            var guidLength = 16;
            var length = 4 + data.Data.Length + guidLength + 4 + messageTypeArray.Length + 4 + sendingPeerArray.Length +
                         (data.SequenceNumber == null ? 0 : 4);
            var finalArray = new byte[length];

            var idByteArray = data.MessageIdentity.ToByteArray();
            idByteArray.CopyTo(finalArray, 0);

            ByteUtils.WriteInt(finalArray, guidLength, messageTypeArray.Length);
            for (int i = 0; i < data.MessageType.Length; i++)
            {
                finalArray[guidLength + 4 + i] = (byte)data.MessageType[i];
            }
            ByteUtils.WriteInt(finalArray, guidLength + 4 + messageTypeArray.Length, sendingPeerArray.Length);
            var sendingPeerOffset = guidLength + messageTypeArray.Length + 4 + 4;
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
            var messageTypeLength = ByteUtils.ReadIntFromStream(data);
            var messageType = ByteUtils.ReadAsciiStringFromStream(data, messageTypeLength);
            var sendingPeerLength = ByteUtils.ReadIntFromStream(data);
            var sendingPeer = ByteUtils.ReadAsciiStringFromStream(data, sendingPeerLength);
            var dataLength = ByteUtils.ReadIntFromStream(data);
            var binaryData = new byte[dataLength];
            data.Read(binaryData, 0, dataLength);
            int? sequenceNumber = null;
            if (data.Length > 16 + 4 + messageTypeLength + 4 + sendingPeerLength + 4 + dataLength)
                sequenceNumber = ByteUtils.ReadIntFromStream(data);

            return new MessageWireData(messageType, id, sendingPeer, binaryData) { SequenceNumber = sequenceNumber };

        }
    }
}
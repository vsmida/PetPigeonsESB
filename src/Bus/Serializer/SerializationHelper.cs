using System;
using System.Collections.Generic;
using Bus.Dispatch;
using Shared;

namespace Bus.Serializer
{
    public interface ISerializationHelper
    {
        int GetMessageTypeId(string messageTypeFullName);
        string GetMessageTypeFromId(int messageTypeId);
    }

    public class SerializationHelper : ISerializationHelper
    {
        private  readonly Dictionary<string, int> _messageTypeToId = new Dictionary<string, int>();
        private  readonly Dictionary<int, string> _messageTypeIdToMessageType = new Dictionary<int, string>();

         public SerializationHelper(IAssemblyScanner scanner)
         {
             var knownMessages = scanner.GetMessageOptions();
             foreach (var messageOptionse in knownMessages)
             {
                 try
                 {
                     var fullName = messageOptionse.MessageType.FullName;
                     var idFromString = StringUtils.CreateIdFromString(fullName);
                     _messageTypeToId.Add(fullName, idFromString);
                     _messageTypeIdToMessageType.Add(idFromString, fullName);

                 }
                 catch (ArgumentException ex)
                 {
                     throw new ArgumentException("Problem while loading message type to message type id dictionary, two type names might have the same id");
                 }
             }
        }

        public int GetMessageTypeId(string messageTypeFullName)
        {
            return _messageTypeToId[messageTypeFullName];
        }

        public string GetMessageTypeFromId(int messageTypeId)
        {
            return _messageTypeIdToMessageType[messageTypeId];
        }



    }
}
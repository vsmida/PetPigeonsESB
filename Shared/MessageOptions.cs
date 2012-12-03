using ProtoBuf;

namespace Shared
{
    [ProtoContract]
    public class MessageOptions
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly ReliabilityInfo ReliabilityInfo;

        public MessageOptions(string messageType, ReliabilityInfo reliabilityInfo)
        {
            MessageType = messageType;
            ReliabilityInfo = reliabilityInfo;
        }

        private MessageOptions(){}
    }
}
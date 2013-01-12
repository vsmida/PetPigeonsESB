using ProtoBuf;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    [ProtoContract]
    public class EndOfPersistedMessages : ICommand
    {
    }
}
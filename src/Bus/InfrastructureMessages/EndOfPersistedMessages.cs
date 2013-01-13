using Bus.MessageInterfaces;
using ProtoBuf;

namespace Bus.InfrastructureMessages
{
    [ProtoContract]
    public class EndOfPersistedMessages : ICommand
    {
    }
}
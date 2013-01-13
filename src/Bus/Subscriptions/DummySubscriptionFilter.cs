using Bus.MessageInterfaces;
using ProtoBuf;

namespace Bus.Subscriptions
{
    [ProtoContract]
    public class DummySubscriptionFilter : ISubscriptionFilter
    {
        public bool Matches(IMessage item)
        {
            return true;
        }
    }
}
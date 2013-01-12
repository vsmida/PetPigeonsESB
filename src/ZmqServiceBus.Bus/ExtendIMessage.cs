using System.Linq;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus
{
    public static class ExtendIMessage
    {
        public static bool IsIEvent(this IMessage message)
        {
            return message.GetType().GetInterfaces().Contains(typeof(IEvent));
        }


        public static bool IsICommand(this IMessage message)
        {
            return message.GetType().GetInterfaces().Contains(typeof(ICommand));
        }
    }
}
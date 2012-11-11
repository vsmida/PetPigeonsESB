using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface ICommandHandler<in T> :IMessageHandler<T> where T : ICommand
    {
    }
}
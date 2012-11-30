using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus
{
    public interface ICommandHandler<in T> :IMessageHandler where T : ICommand
    {
    }
}
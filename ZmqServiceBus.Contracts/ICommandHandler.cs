namespace ZmqServiceBus.Contracts
{
    public interface ICommandHandler<in T> :IMessageHandler<T> where T : ICommand
    {
    }
}
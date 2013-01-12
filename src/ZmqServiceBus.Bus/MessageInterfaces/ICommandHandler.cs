namespace ZmqServiceBus.Bus.MessageInterfaces
{
    public interface ICommandHandler<in T> :IMessageHandler where T : ICommand
    {
        void Handle(T item);
    }
}
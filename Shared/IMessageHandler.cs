namespace Shared
{
    public interface IMessageHandler<in T> : IMessageHandler where T : IMessage
    {
        void Handle(T message);
    }

    public interface IMessageHandler { }

}
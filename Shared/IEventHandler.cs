namespace Shared
{
    public interface IEventHandler<T> where T : IEvent
    {
        void Handle(T message);
    }
}
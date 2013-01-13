using Bus.MessageInterfaces;

namespace Bus.Dispatch
{
    public interface IMessageDispatcher
    {
        void Dispatch(IMessage message);
    }
}
using Bus.MessageInterfaces;

namespace Bus.Dispatch
{
    interface IMessageDispatcher
    {
        void Dispatch(IMessage message);
    }
}
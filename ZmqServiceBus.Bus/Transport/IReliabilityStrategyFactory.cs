using Shared;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IReliabilityStrategyFactory
    {
        ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions);
        IStartupReliabilityStrategy GetStartupStrategy(MessageOptions messageOptions, string peerName, string messageType);
    }
}
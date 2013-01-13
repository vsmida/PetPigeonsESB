using Bus.Transport.SendingPipe.SendingStrategies;

namespace Bus.Transport
{
    public interface IReliabilityStrategyFactory
    {
        ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions);
    }
}
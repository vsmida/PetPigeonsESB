namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates
{
    public interface ISendingStrategyStateManager
    {
        void CheckMessage(IReceivedTransportMessage transportMessage);
        void RegisterStrategy(ISendingReliabilityStrategyState state);
    }
}
using System;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IReliabilityStrategyFactory : IDisposable
    {
        ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions);
        IStartupReliabilityStrategy GetStartupStrategy(MessageOptions messageOptions, string peerName, string messageType, IPersistenceSynchronizer synchronizer);
    }
}
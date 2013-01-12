using System;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IReliabilityStrategyFactory
    {
        ISendingReliabilityStrategy GetSendingStrategy(MessageOptions messageOptions);
    }
}
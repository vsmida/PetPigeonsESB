using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;
using IReceivedTransportMessage = ZmqServiceBus.Bus.Transport.IReceivedTransportMessage;

namespace ZmqServiceBus.Bus
{
    public class InternalBus : IBus, IReplier
    {
        private readonly IReceptionLayer _startupLayer;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMessageSender _messageSender;
        private readonly IBusBootstrapper _busBootstrapper;
        private readonly Dictionary<Type, bool> _messageTypeToInfrastructureCondition = new Dictionary<Type, bool>();
        private readonly BlockingCollection<IReceivedTransportMessage> _standardMessagesToDispatch = new BlockingCollection<IReceivedTransportMessage>();
        private volatile bool _running = true;
        private Thread _standardDispatchThread;

        public InternalBus(IReceptionLayer startupLayer, IMessageDispatcher dispatcher, IMessageSender messageSender, IBusBootstrapper busBootstrapper)
        {
            _startupLayer = startupLayer;
            _dispatcher = dispatcher;
            _messageSender = messageSender;
            _busBootstrapper = busBootstrapper;
        }

        public IBlockableUntilCompletion Send(ICommand command)
        {
            var blockableUntilCompletion = _messageSender.Send(command);
            return blockableUntilCompletion;
        }

        public void Publish(IEvent message)
        {
            _messageSender.Publish(message);
        }

        public void Initialize()
        {
            _startupLayer.Initialize();
            _startupLayer.OnMessageReceived += OnTransportMessageReceived;
            _busBootstrapper.BootStrapTopology();

            _standardDispatchThread = new Thread(() =>
                                                     {
                                                         while (_running)
                                                         {
                                                             IReceivedTransportMessage transportMessage;
                                                             if (_standardMessagesToDispatch.TryTake(out transportMessage,
                                                                                                     TimeSpan.FromMilliseconds(100)))
                                                                 DoDispatch(transportMessage);
                                                         }

                                                     });
            _standardDispatchThread.Start();
        }

        public void Reply(IMessage message)
        {
            _messageSender.Route(message, MessageContext.PeerName);
        }

        private void OnTransportMessageReceived(IReceivedTransportMessage receivedTransportMessage)
        {
            var messageType = TypeUtils.Resolve(receivedTransportMessage.MessageType);
            bool isInfrastructure = false;
            if (!_messageTypeToInfrastructureCondition.ContainsKey(messageType))
            {
                isInfrastructure = IsInfrastructure(messageType);
                _messageTypeToInfrastructureCondition[messageType] = isInfrastructure;
            }

            if (isInfrastructure)
                DoDispatch(receivedTransportMessage);
            else
                _standardMessagesToDispatch.TryAdd(receivedTransportMessage);
        }

        private void DoDispatch(IReceivedTransportMessage receivedTransportMessage)
        {
            bool successfulDispatch = true;
            try
            {
                var deserializedMessage = Serializer.Deserialize(receivedTransportMessage.Data, TypeUtils.Resolve(receivedTransportMessage.MessageType));
                _dispatcher.Dispatch(deserializedMessage as IMessage);
            }
            catch (Exception)
            {
                successfulDispatch = false;
            }
            if (receivedTransportMessage.MessageType != typeof(CompletionAcknowledgementMessage).FullName)
                _messageSender.Route(
                    new CompletionAcknowledgementMessage(receivedTransportMessage.MessageIdentity, successfulDispatch),
                    receivedTransportMessage.PeerName);
        }

        private static bool IsInfrastructure(Type type)
        {
            return type.GetCustomAttributes(typeof(InfrastructureMessageAttribute), true).Any();
        }
        public void Dispose()
        {
            _running = false;
            _startupLayer.Dispose();
            _messageSender.Dispose();
            _standardMessagesToDispatch.CompleteAdding();
            _standardDispatchThread.Join();
        }
    }
}
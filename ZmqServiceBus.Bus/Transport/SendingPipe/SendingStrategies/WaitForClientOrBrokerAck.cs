using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.Handlers;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{

 
    internal class WaitForClientOrBrokerAck : SendingReliabilityStrategy
    {
        private readonly IEndpoint _brokerEndpoint;
        private readonly ISendingStrategyStateManager _stateManager;
        private readonly IPersistenceSynchronizer _persistenceSynchronizer;
        public override event Action ReliabilityAchieved = delegate{};

        //todo: special case when acknowledgement message. special message to broker to flush from queue? only for routing?
        public WaitForClientOrBrokerAck(IEndpoint brokerEndpoint, ISendingStrategyStateManager stateManager)
        {
            _brokerEndpoint = brokerEndpoint;
            _stateManager = stateManager;
        }

        public override IEnumerable<ISendingBusMessage> Send(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {

            ISendingBusMessage sendingMessage = GetTransportMessage(message, concernedSubscriptions.Select(x => x.Endpoint));
            //ISendingBusMessage brokerMessage;
            
            //if (message.GetType() == typeof(CompletionAcknowledgementMessage))
            //{
            //    var completionMessage = message as CompletionAcknowledgementMessage;
            //    brokerMessage =
            //        GetTransportMessage(new ForgetMessageCommand(message.GetType().FullName, completionMessage.MessageId),new[]{_brokerEndpoint});

            //}
            //else
            //    brokerMessage = GetTransportMessage(new PersistMessageCommand(sendingMessage), new[] { _brokerEndpoint });

            var completionCallback = _persistenceSynchronizer.PersistMessage(sendingMessage);
            

            var strategyState = new WaitForAckState(new[] {sendingMessage.MessageIdentity});

            _stateManager.RegisterStrategy(strategyState);
            completionCallback.MessageReliablySent += ReliabilityAchieved;
            strategyState.WaitConditionFulfilled += ReliabilityAchieved;
            return new[] {sendingMessage};
        }

        public override IEnumerable<ISendingBusMessage> Publish(IMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions)
        {
            return null;
          //  var brokerMessage = new SendingBusMessage(message.MessageType, message.MessageIdentity, Serializer.Serialize(new PersistMessageCommand(message)));

         //   var strategyStateBroker = new WaitForAckState(brokerMessage.MessageIdentity);
   //         var strategyStateMessage = new PublishWaitForAckState(message.MessageIdentity, concernedSubscriptions.Select(x => x.Peer));

           // _stateManager.RegisterStrategy(strategyStateBroker);
     //       _stateManager.RegisterStrategy(strategyStateMessage);

       //     _dataSender.SendMessage(brokerMessage, _brokerEndpoint);
      //      foreach (var endpoint in concernedSubscriptions.Select(x => x.Endpoint))
      //      {
        //        _dataSender.SendMessage(message, endpoint);
         //   }
       //     WaitHandle.WaitAny(new[] { strategyStateBroker.WaitHandle, strategyStateMessage.WaitHandle });
        }
    }
}
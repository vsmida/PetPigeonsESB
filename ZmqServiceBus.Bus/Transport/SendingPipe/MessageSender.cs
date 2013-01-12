using System;
using System.Collections.Generic;
using Disruptor;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;
using System.Linq;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public class MessageSender : IMessageSender
    {
        private RingBuffer<OutboundDisruptorEntry> _ringBuffer;
        private readonly IPeerConfiguration _peerConfiguration;

        public MessageSender(IPeerConfiguration peerConfiguration)
        {
            _peerConfiguration = peerConfiguration;
        }

        public void SendHeartbeat(IEndpoint endpoint)
        {
            var sequence = _ringBuffer.Next();
            var data = _ringBuffer[sequence];

            var heartbeatRequest = new HeartbeatRequest(DateTime.UtcNow, endpoint);
            var serializedMessage = BusSerializer.Serialize(heartbeatRequest);
            var messageWireData = new MessageWireData(typeof (HeartbeatRequest).FullName, Guid.NewGuid(), _peerConfiguration.PeerName, serializedMessage);
            data.NetworkSenderData.WireMessages.Add(new WireSendingMessage(messageWireData,endpoint));

            _ringBuffer.Publish(sequence);
        }

        public void Initialize(RingBuffer<OutboundDisruptorEntry> buffer)
        {
            _ringBuffer = buffer;
        }

        public ICompletionCallback Send(ICommand message, ICompletionCallback callback = null)
        {
            var nonNullCallback = callback ?? new DefaultCompletionCallback();
            SendInternal(message, nonNullCallback);
            return nonNullCallback;
        }

        private void SendInternal(IMessage message, ICompletionCallback callback)
        {
            var sequence = _ringBuffer.Next();
            var data = _ringBuffer[sequence];
            data.MessageTargetHandlerData.Message = message;
            data.MessageTargetHandlerData.Callback = callback;
            _ringBuffer.Publish(sequence);
        }

        public void Publish(IEvent message)
        {
            SendInternal(message, null);
        }

        public ICompletionCallback Route(IMessage message, string peerName)
        {
            var callback = new DefaultCompletionCallback();

            var sequence = _ringBuffer.Next();
            var data = _ringBuffer[sequence];

            data.MessageTargetHandlerData.Message = message;
            data.MessageTargetHandlerData.Callback = callback;
            data.MessageTargetHandlerData.TargetPeer = peerName;
            
            _ringBuffer.Publish(sequence);
            
            return callback;
        }

        public void Acknowledge(Guid messageId,string messageType, bool processSuccessful, string originatingPeer, WireTransportType transportType)
        {
            var acknowledgementMessage = new CompletionAcknowledgementMessage(messageId,messageType, processSuccessful, transportType);
            var sequence = _ringBuffer.Next();
            var data = _ringBuffer[sequence];

            data.MessageTargetHandlerData.Message = acknowledgementMessage;
            data.MessageTargetHandlerData.TargetPeer = originatingPeer;
            data.MessageTargetHandlerData.IsAcknowledgement = true;
            
            _ringBuffer.Publish(sequence);
        }
    }
}
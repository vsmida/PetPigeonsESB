using System;
using Bus.InfrastructureMessages;
using Bus.InfrastructureMessages.Heartbeating;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Disruptor;

namespace Bus.Transport.SendingPipe
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
            data.MessageTargetHandlerData = new MessageTargetHandlerData();
            var heartbeatRequest = new HeartbeatRequest(DateTime.UtcNow, endpoint);
            var serializedMessage = BusSerializer.Serialize(heartbeatRequest);
            var messageWireData = new MessageWireData(typeof (HeartbeatRequest).FullName, Guid.NewGuid(), _peerConfiguration.PeerName, serializedMessage);
            data.NetworkSenderData.WireMessages.Clear();
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
            data.MessageTargetHandlerData.TargetPeer = null;
            data.MessageTargetHandlerData.IsAcknowledgement = false;
            data.NetworkSenderData = new NetworkSenderData();
            
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
            data.MessageTargetHandlerData.IsAcknowledgement = false;
            data.NetworkSenderData = new NetworkSenderData();

            
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
            data.MessageTargetHandlerData.Callback = null;
            data.NetworkSenderData = new NetworkSenderData();

            
            _ringBuffer.Publish(sequence);
        }

        public void Dispose()
        {
        }
    }
}
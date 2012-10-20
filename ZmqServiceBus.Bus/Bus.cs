using Shared;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Bus
{
    public class Bus : IBus
    {
        private readonly ITransport _transport;


        public Bus(ITransport transport)
        {
            _transport = transport;
            
        }

        public void Send(ICommand command)
        {
            _transport.SendMessage(command);
        }

        public void Publish(IEvent message)
        {
            _transport.PublishMessage(message);
        }

        public void Initialize()
        {
            _transport.Initialize();
            _transport.OnMessageReceived += OnTransportMessageReceived;
        }

        private void OnTransportMessageReceived(ITransportMessage transportMessage)
        {
            //transportMessage
        }
    }
}
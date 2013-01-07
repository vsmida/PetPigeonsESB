using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IBlockableUntilMessageReliablySent
    {
        void WaitForMessageToBeReliablySent();
        event Action MessageReliablySent;
        void Release();
    }

    public interface IPersistenceSynchronizer
    {
        void PersistMessages(IEnumerable<SendingBusMessage> messages);
    }

    public class PeerQueue
    {
        private Queue<SendingBusMessage> _messages = new Queue<SendingBusMessage>();
        public string PeerName { get; private set; }
        public Queue<SendingBusMessage> Messages
        {
            get { return _messages; }
        }


        public PeerQueue(string peerName)
        {
            PeerName = peerName;
        }

        public void AddMessage(SendingBusMessage message)
        {
            _messages.Enqueue(message);
        }
    }

    public class InMemoryPersistenceSynchronizer
    {


        private IPeerManager _peerManager;
        private Dictionary<IEndpoint, string> _endpointsToPeers = new Dictionary<IEndpoint, string>();
        private ConcurrentDictionary<string, ConcurrentQueue<SendingBusMessage>> _messagesByPeer = new ConcurrentDictionary<string, ConcurrentQueue<SendingBusMessage>>();
        private ConcurrentDictionary<IEndpoint, ConcurrentQueue<SendingBusMessage>> _endpointsToSavedMessages = new ConcurrentDictionary<IEndpoint, ConcurrentQueue<SendingBusMessage>>();
        private IDataSender _dataSender;

        public InMemoryPersistenceSynchronizer(IPeerManager peerManager, IDataSender dataSender)
        {
            _peerManager = peerManager;
            _dataSender = dataSender;
        }

    }
}
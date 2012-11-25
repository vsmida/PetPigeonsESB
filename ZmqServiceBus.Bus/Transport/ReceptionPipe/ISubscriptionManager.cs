using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public interface ISubscriptionManager
    {
        event Action<Type> NewEventSubscription;
        event Action<Type> EventUnsubscibe;
        IDisposable StartListeningTo<T>() where T : IEvent;

    }

    public class SubscriptionManager : ISubscriptionManager
    {
        public event Action<Type> NewEventSubscription = delegate{};
        public event Action<Type> EventUnsubscibe = delegate{};

        private HashSet<Type> _subscriptions = new HashSet<Type>();
        private IPeerManager _peerManager;

        public SubscriptionManager(IPeerManager peerManager)
        {
            _peerManager = peerManager;
            peerManager.PeerConnected += OnPeerConnected;
        }

        private void OnPeerConnected(IServicePeer peer)
        {
            foreach (var type in peer.PublishedMessages ?? new List<Type>())
            {
                if (_subscriptions.Contains(type))
                    NewEventSubscription(type);
            }
        }

        public IDisposable StartListeningTo<T>() where T : IEvent
        {
            _subscriptions.Add(typeof(T));
            NewEventSubscription(typeof(T));
            return new DisposableAction(() => EventUnsubscibe(typeof(T)));
        }
    }
}
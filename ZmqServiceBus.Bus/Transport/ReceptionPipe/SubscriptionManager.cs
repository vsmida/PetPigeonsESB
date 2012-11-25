using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class SubscriptionManager : ISubscriptionManager
    {
        public event Action<Type> NewEventSubscription = delegate{};
        public event Action<Type> EventUnsubscibe = delegate{};

        private readonly HashSet<Type> _subscriptions = new HashSet<Type>();

        public SubscriptionManager(IPeerManager peerManager)
        {
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
using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Subscriptions
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

        private void OnPeerConnected(ServicePeer peer)
        {
           
        }

        public IDisposable StartListeningTo<T>() where T : IEvent
        {
            return StartListeningTo(typeof (T));
        }

        public IDisposable StartListeningTo(Type eventType)
        {
            if (!(typeof(IEvent).IsAssignableFrom(eventType)))
                throw new ArgumentException("Type is not an event");

            _subscriptions.Add(eventType);
            NewEventSubscription(eventType);
            return new DisposableAction(() => EventUnsubscibe(eventType));
        }
    }
}
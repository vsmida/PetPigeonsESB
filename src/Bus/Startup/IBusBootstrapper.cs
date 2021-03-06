using System;
using System.Collections.Generic;
using System.Linq;
using Bus.Dispatch;
using Bus.InfrastructureMessages;
using Bus.InfrastructureMessages.Topology;
using Bus.Subscriptions;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using Shared;
using log4net;

namespace Bus.Startup
{
    interface IBusBootstrapper
    {
        void BootStrapTopology();
    }

    class BusBootstrapper : IBusBootstrapper
    {
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly ZmqTransportConfiguration _zmqTransportConfiguration;
        private readonly IBusBootstrapperConfiguration _bootstrapperConfiguration;
        private readonly IMessageSender _messageSender;
        private readonly IPeerManager _peerManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly ILog _logger = LogManager.GetLogger(typeof(BusBootstrapper));

        public BusBootstrapper(IAssemblyScanner assemblyScanner, ZmqTransportConfiguration zmqTransportConfiguration, IBusBootstrapperConfiguration bootstrapperConfiguration,
            IMessageSender messageSender, IPeerManager peerManager, ISubscriptionManager subscriptionManager, IPeerConfiguration peerConfiguration)
        {
            _assemblyScanner = assemblyScanner;
            _zmqTransportConfiguration = zmqTransportConfiguration;
            _bootstrapperConfiguration = bootstrapperConfiguration;
            _messageSender = messageSender;
            _peerManager = peerManager;
            _subscriptionManager = subscriptionManager;
            _peerConfiguration = peerConfiguration;
        }

        public void BootStrapTopology()
        {
            var handledTypes = new HashSet<Type>(_assemblyScanner.GetHandledCommands().Union(_assemblyScanner.GetHandledEvents()));

            var messageSubscriptions = _assemblyScanner.GetMessageOptions().Where(x => handledTypes.Contains(x.MessageType))
                .Select(x => new MessageSubscription(x.MessageType, _peerConfiguration.PeerId,
                                            new ZmqEndpoint(_zmqTransportConfiguration.GetConnectEndpoint()),
                                            GetSubscription(x), x.ReliabilityLevel));




            var peer = new ServicePeer(_peerConfiguration.PeerName,_peerConfiguration.PeerId, messageSubscriptions.ToList(), _peerConfiguration.ShadowedPeers);
            var commandRequest = new InitializeTopologyRequest(peer);

            var directoryServiceRegisterPeerSubscription = new MessageSubscription(typeof(InitializeTopologyRequest),
                                                                                   _bootstrapperConfiguration.
                                                                                       DirectoryServiceId,
                                                                                   new ZmqEndpoint(
                                                                                       _bootstrapperConfiguration.
                                                                                           DirectoryServiceEndpoint),
                                                                                   null, Shared.ReliabilityLevel.FireAndForget);

            var directoryServiceRegisterPeerSubscription2 = new MessageSubscription(typeof(InitializeTopologyAndMessageSettings),
                                                                       _bootstrapperConfiguration.
                                                                           DirectoryServiceId,
                                                                       new ZmqEndpoint(
                                                                           _bootstrapperConfiguration.
                                                                               DirectoryServiceEndpoint),
                                                                       null, Shared.ReliabilityLevel.FireAndForget);


            var directoryServiceCompletionMessageSubscription = new MessageSubscription(typeof(CompletionAcknowledgementMessage),
                                                                       _bootstrapperConfiguration.
                                                                           DirectoryServiceId,
                                                                       new ZmqEndpoint(
                                                                           _bootstrapperConfiguration.
                                                                               DirectoryServiceEndpoint),
                                                                       null, Shared.ReliabilityLevel.FireAndForget);

            var directoryServiceBarebonesPeer = new ServicePeer(_bootstrapperConfiguration.DirectoryServiceName, _bootstrapperConfiguration.DirectoryServiceId,
                                                                new List<MessageSubscription> { directoryServiceRegisterPeerSubscription, directoryServiceCompletionMessageSubscription, directoryServiceRegisterPeerSubscription2 }, null);

            _peerManager.RegisterPeerConnection(directoryServiceBarebonesPeer);
            _peerManager.RegisterPeerConnection(peer); //register yourself after dir service in case dirService=Service;

            _logger.InfoFormat("Requesting topology from {0}", _bootstrapperConfiguration.DirectoryServiceName);
            var completionCallback = _messageSender.Route(commandRequest, _bootstrapperConfiguration.DirectoryServiceId);
            completionCallback.WaitForCompletion(); //now should get a init topo (or not) reply and the magic is done?

            //now register with everybody we know of
            _messageSender.Publish(new PeerConnected(peer));


            var persistenceShadowPeer = (_peerManager.PeersThatShadowMe() ?? Enumerable.Empty<ServicePeerShadowInformation>()).SingleOrDefault(x => x.IsPersistenceProvider);
            if (persistenceShadowPeer != null)
            {
                _logger.InfoFormat("Requesting missed messages for {0}", _peerConfiguration.PeerName);
                _messageSender.Route(new SynchronizeWithBrokerCommand(_peerConfiguration.PeerId), persistenceShadowPeer.ServicePeer.PeerId).WaitForCompletion();
            }

            //ask for topo again in case someone connected simulataneously to other node
            //    completionCallback = _messageSender.Route(commandRequest, _bootstrapperConfiguration.DirectoryServiceName);
            //    completionCallback.WaitForCompletion(); //now should get a init topo (or not) reply and the magic is done?


        }

        private ISubscriptionFilter GetSubscription(MessageOptions options)
        {
            if (options.MessageType == typeof(SynchronizeWithBrokerCommand) || options.MessageType == typeof(StopSynchWithBrokerCommand))
            {
                List<PeerId> acceptedPeers = new List<PeerId>();
                if(_peerConfiguration.ShadowedPeers != null)
                acceptedPeers = _peerConfiguration.ShadowedPeers.Where(x => x.IsPersistenceProvider).Select(x => x.PeerPeerId).ToList();
                return new SynchronizeWithBrokerFilter(acceptedPeers); // todo: implement dynamic subscriptions
            }
            return options.SubscriptionFilter;


        }
    }
}
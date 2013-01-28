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
        private readonly IMessageOptionsRepository _optionsRepo;
        private readonly IMessageSender _messageSender;
        private readonly IPeerManager _peerManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly ILog _logger = LogManager.GetLogger(typeof (BusBootstrapper));

        public BusBootstrapper(IAssemblyScanner assemblyScanner, ZmqTransportConfiguration zmqTransportConfiguration, IBusBootstrapperConfiguration bootstrapperConfiguration,
            IMessageOptionsRepository optionsRepo, IMessageSender messageSender, IPeerManager peerManager, ISubscriptionManager subscriptionManager, IPeerConfiguration peerConfiguration)
        {
            _assemblyScanner = assemblyScanner;
            _zmqTransportConfiguration = zmqTransportConfiguration;
            _bootstrapperConfiguration = bootstrapperConfiguration;
            _optionsRepo = optionsRepo;
            _messageSender = messageSender;
            _peerManager = peerManager;
            _subscriptionManager = subscriptionManager;
            _peerConfiguration = peerConfiguration;
        }

        public void BootStrapTopology()
        {
            _optionsRepo.InitializeOptions();

            var messageSubscriptions =
                _assemblyScanner.GetHandledCommands().Concat(_assemblyScanner.GetHandledEvents()).Select(
                    x =>
                    new MessageSubscription(x, _peerConfiguration.PeerName,
                                            new ZmqEndpoint(_zmqTransportConfiguration.GetConnectEndpoint()), GetSubscription(x)));


            var peer = new ServicePeer(_peerConfiguration.PeerName, messageSubscriptions.ToList(), _peerConfiguration.ShadowedPeers);
            var commandRequest = new InitializeTopologyRequest(peer);

            var directoryServiceRegisterPeerSubscription = new MessageSubscription(typeof(InitializeTopologyRequest),
                                                                                   _bootstrapperConfiguration.
                                                                                       DirectoryServiceName,
                                                                                   new ZmqEndpoint(
                                                                                       _bootstrapperConfiguration.
                                                                                           DirectoryServiceEndpoint),
                                                                                   null);

            var directoryServiceRegisterPeerSubscription2 = new MessageSubscription(typeof(InitializeTopologyAndMessageSettings),
                                                                       _bootstrapperConfiguration.
                                                                           DirectoryServiceName,
                                                                       new ZmqEndpoint(
                                                                           _bootstrapperConfiguration.
                                                                               DirectoryServiceEndpoint),
                                                                       null);


            var directoryServiceCompletionMessageSubscription = new MessageSubscription(typeof(CompletionAcknowledgementMessage),
                                                                       _bootstrapperConfiguration.
                                                                           DirectoryServiceName,
                                                                       new ZmqEndpoint(
                                                                           _bootstrapperConfiguration.
                                                                               DirectoryServiceEndpoint),
                                                                       null);

            var directoryServiceBarebonesPeer = new ServicePeer(_bootstrapperConfiguration.DirectoryServiceName,
                                                                new List<MessageSubscription> { directoryServiceRegisterPeerSubscription, directoryServiceCompletionMessageSubscription, directoryServiceRegisterPeerSubscription2 }, null);
          
            _peerManager.RegisterPeerConnection(directoryServiceBarebonesPeer);
            _peerManager.RegisterPeerConnection(peer); //register yourself after dir service in case dirService=Service;

            _logger.InfoFormat("Requesting topology from {0}", _bootstrapperConfiguration.DirectoryServiceName);
            var completionCallback = _messageSender.Route(commandRequest, _bootstrapperConfiguration.DirectoryServiceName);
            completionCallback.WaitForCompletion(); //now should get a init topo (or not) reply and the magic is done?
            
            //now register with everybody we know of
            _messageSender.Publish(new PeerConnected(peer));


            if ((_peerManager.PeersThatShadowMe() ?? Enumerable.Empty<ServicePeer>()).Any())
            {
                _logger.InfoFormat("Requesting missed messages for {0}", _peerConfiguration.PeerName);
                _messageSender.Send(new SynchronizeWithBrokerCommand(_peerConfiguration.PeerName)).WaitForCompletion();
            }

            //ask for topo again in case someone connected simulataneously to other node
        //    completionCallback = _messageSender.Route(commandRequest, _bootstrapperConfiguration.DirectoryServiceName);
        //    completionCallback.WaitForCompletion(); //now should get a init topo (or not) reply and the magic is done?

        
        }

        private ISubscriptionFilter GetSubscription(Type type)
        {
            if(type == typeof(SynchronizeWithBrokerCommand) || type == typeof(StopSynchWithBrokerCommand))
                return new SynchronizeWithBrokerFilter(_peerConfiguration.ShadowedPeers);
            return null;


        }
    }
}
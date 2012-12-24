using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using System.Linq;

namespace ZmqServiceBus.Bus.Startup
{
    public interface IBusBootstrapper
    {
        void BootStrapTopology();
    }

    public class BusBootstrapper : IBusBootstrapper
    {
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly ZmqTransportConfiguration _zmqTransportConfiguration;
        private readonly IBusBootstrapperConfiguration _bootstrapperConfiguration;
        private readonly IMessageOptionsRepository _optionsRepo;
        private readonly IMessageSender _messageSender;
        private readonly IPeerManager _peerManager;
        private readonly ISubscriptionManager _subscriptionManager;

        public BusBootstrapper(IAssemblyScanner assemblyScanner, ZmqTransportConfiguration zmqTransportConfiguration, IBusBootstrapperConfiguration bootstrapperConfiguration,
            IMessageOptionsRepository optionsRepo, IMessageSender messageSender, IPeerManager peerManager, ISubscriptionManager subscriptionManager)
        {
            _assemblyScanner = assemblyScanner;
            _zmqTransportConfiguration = zmqTransportConfiguration;
            _bootstrapperConfiguration = bootstrapperConfiguration;
            _optionsRepo = optionsRepo;
            _messageSender = messageSender;
            _peerManager = peerManager;
            _subscriptionManager = subscriptionManager;
        }

        public void BootStrapTopology()
        {
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(InitializeTopologyAndMessageSettings).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(RegisterPeerCommand).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(PeerConnected).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));
            _optionsRepo.RegisterOptions(new MessageOptions(typeof(CompletionAcknowledgementMessage).FullName, new ReliabilityInfo(ReliabilityLevel.FireAndForget)));

            var messageSubscriptions =
                _assemblyScanner.GetHandledCommands().Concat(_assemblyScanner.GetHandledEvents()).Select(
                    x =>
                    new MessageSubscription(x, _zmqTransportConfiguration.PeerName,
                                            new ZmqEndpoint(_zmqTransportConfiguration.GetConnectEndpoint()), null));

            var peer = new ServicePeer(_zmqTransportConfiguration.PeerName, messageSubscriptions);
            var command = new RegisterPeerCommand(peer);

            var directoryServiceRegisterPeerSubscription = new MessageSubscription(typeof (RegisterPeerCommand),
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
                                                                new List<MessageSubscription> { directoryServiceRegisterPeerSubscription, directoryServiceCompletionMessageSubscription });
            _peerManager.RegisterPeer(directoryServiceBarebonesPeer);

            _messageSender.Send(command).WaitForCompletion(); //now should get a init topo reply and the magic is done?
        }
    }
}
using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Startup
{
    public interface IBusBootstrapper
    {
        void BootStrapTopology();
    }

    public class BusBootstrapper : IBusBootstrapper
    {
        private readonly IAssemblyScanner _assemblyScanner;
        private readonly TransportConfiguration _transportConfiguration;
        private readonly IBusBootstrapperConfiguration _bootstrapperConfiguration;
        private readonly IMessageOptionsRepository _optionsRepo;
        private readonly IMessageSender _messageSender;
        private readonly IPeerManager _peerManager;
        private readonly ISubscriptionManager _subscriptionManager;

        public BusBootstrapper(IAssemblyScanner assemblyScanner, TransportConfiguration transportConfiguration, IBusBootstrapperConfiguration bootstrapperConfiguration,
            IMessageOptionsRepository optionsRepo, IMessageSender messageSender, IPeerManager peerManager, ISubscriptionManager subscriptionManager)
        {
            _assemblyScanner = assemblyScanner;
            _transportConfiguration = transportConfiguration;
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

           var peer = new ServicePeer(_transportConfiguration.PeerName, _transportConfiguration.GetCommandsConnectEnpoint(),
                                       _transportConfiguration.GetEventsConnectEndpoint(),
                                       _assemblyScanner.GetHandledCommands(), _assemblyScanner.GetSentEvents());
            var command = new RegisterPeerCommand(peer);
            var directoryServiceBarebonesPeer = new ServicePeer(_bootstrapperConfiguration.DirectoryServiceName, _bootstrapperConfiguration.DirectoryServiceCommandEndpoint,
                                                                _bootstrapperConfiguration.DirectoryServiceEventEndpoint, new List<Type> { typeof(RegisterPeerCommand) }, new List<Type> { typeof(PeerConnected) });
            _peerManager.RegisterPeer(directoryServiceBarebonesPeer);

            foreach (var handledEvent in _assemblyScanner.GetHandledEvents())
            {
                _subscriptionManager.StartListeningTo(handledEvent);
            }

            _messageSender.Send(command).WaitForCompletion(); //now should get a init topo reply and the magic is done?
        }
    }
}
﻿using StructureMap.Configuration.DSL;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBusHost
{
    public class ServiceRegistry : Registry
    {
         public ServiceRegistry()
         {
             For<IObjectFactory>().Use<ObjectFactory>();
             For<IZmqSocketManager>().Use<ZmqSocketManager>();
             For<IEndpointManager>().Use<EndpointManager>();
             For<IReceptionLayer>().Use<ReceptionLayer>();


         }
    }
}
﻿using System;
using StructureMap;
using ZmqServiceBus.Bus.Startup;

namespace ZmqServiceBus.Bus
{

    public static class BusFactory
    {

        public static IBus CreateBus(IContainer container = null, Action<ConfigurationExpression> containerConfigurationExpression = null)
        {
            IContainer containerForBus = container ??  new Container(new BusRegistry());
            if(containerConfigurationExpression != null)
            containerForBus.Configure(containerConfigurationExpression);
            var bus = containerForBus.GetInstance<IBus>();
            return bus;
          
        }

    }
}
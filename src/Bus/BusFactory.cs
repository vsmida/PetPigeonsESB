using System;
using System.IO;
using Bus.Startup;
using StructureMap;

namespace Bus
{

    public static class BusFactory
    {

        public static IBus CreateBus(IContainer container = null, Action<ConfigurationExpression> containerConfigurationExpression = null)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("Log4net.config"));
            IContainer containerForBus;
            if (container != null)
            {
                container.Configure(ctx => ctx.AddRegistry(new BusRegistry()));
                containerForBus = container;
            }
            else containerForBus = new Container(new BusRegistry());
            if (containerConfigurationExpression != null)
                containerForBus.Configure(containerConfigurationExpression);
            var bus = containerForBus.GetInstance<IBus>();
            return bus;

        }

    }
}
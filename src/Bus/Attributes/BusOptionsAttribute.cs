using System;
using Bus.Transport.Network;
using Shared;

namespace Bus.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BusOptionsAttribute : Attribute
    {
        public readonly ReliabilityLevel ReliabilityLevel;
        public readonly WireTransportType TransportType;

        public BusOptionsAttribute(ReliabilityLevel reliabilityLevel, WireTransportType transportType)
        {
            ReliabilityLevel = reliabilityLevel;
            TransportType = transportType;
        }
    }



}

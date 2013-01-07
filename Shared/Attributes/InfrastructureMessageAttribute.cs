using System;

namespace Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InfrastructureMessageAttribute : Attribute
    {
         
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class BusReliability : Attribute
    {
        public readonly ReliabilityLevel ReliabilityLevel;

        public BusReliability(ReliabilityLevel reliabilityLevel)
        {
            ReliabilityLevel = reliabilityLevel;
        }
    }
}
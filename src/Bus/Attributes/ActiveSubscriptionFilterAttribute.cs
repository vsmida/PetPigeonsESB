using System;

namespace Bus.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SubscriptionFilterAttributeActive : Attribute
    {
        public bool Active { get; private set; }

        public SubscriptionFilterAttributeActive(bool active)
        {
            Active = active;
        }
    }
}
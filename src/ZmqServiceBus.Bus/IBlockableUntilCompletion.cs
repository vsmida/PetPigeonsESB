using System;

namespace ZmqServiceBus.Bus
{
    public interface IBlockableUntilCompletion
    {
        void WaitForCompletion(TimeSpan timeout);        
        void WaitForCompletion();        
    }
}
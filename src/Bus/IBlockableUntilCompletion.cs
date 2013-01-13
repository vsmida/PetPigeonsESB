using System;

namespace Bus
{
    public interface IBlockableUntilCompletion
    {
        void WaitForCompletion(TimeSpan timeout);        
        void WaitForCompletion();        
    }
}
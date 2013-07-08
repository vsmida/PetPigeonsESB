using System.Diagnostics;

namespace Shared
{
    public static class StopWatchExtensions
    {
         public static int ElapsedMicroseconds(this Stopwatch watch)
         {
             var elapsedMicroseconds = (watch.ElapsedTicks/((double) Stopwatch.Frequency))*1000000;
             return (int)elapsedMicroseconds;
         }
    }
}
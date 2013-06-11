using System;
using System.Diagnostics;

namespace Shared
{
    public class PerformanceMeasure : IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly int _timesToRun;
        private int _gc0;
        private int _gc1;
        private int _gc2;

        public PerformanceMeasure(Action codeToTest, int timesToRun, Stopwatch watch = null)
        {
            _watch = watch ?? new Stopwatch();
            _timesToRun = timesToRun;
            _gc0 = GC.CollectionCount(0);
            _gc1 = GC.CollectionCount(1);
            _gc2 = GC.CollectionCount(2);
            _watch.Start();
            for (int i = 0; i < timesToRun; i++)
            {
                codeToTest();
            }
        }



        public void Dispose()
        {
            _watch.Stop();
            var fps = _timesToRun / (_watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps.ToString("N2"));

            var gc0 = GC.CollectionCount(0) - _gc0;
            var gc1 = GC.CollectionCount(1) - _gc1;
            var gc2 = GC.CollectionCount(2) - _gc2;

            Console.WriteLine("GC 0 " + gc0);
            Console.WriteLine("GC 1 " + gc1);
            Console.WriteLine("GC 2 " + gc2);
        }
    }
}
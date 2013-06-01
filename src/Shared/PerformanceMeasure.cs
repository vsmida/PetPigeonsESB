using System;
using System.Diagnostics;

namespace Shared
{
    public class PerformanceMeasure : IDisposable
    {
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly int _timesToRun;

        public PerformanceMeasure(Action codeToTest, int timesToRun)
        {
            _timesToRun = timesToRun;
            _watch.Start();
            for (int i = 0; i < timesToRun; i++)
            {
                codeToTest();
            }
            _watch.Stop();
        }



        public void Dispose()
        {
            var fps = _timesToRun / (_watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(" FPS : " + fps.ToString("N2"));
        }
    }
}
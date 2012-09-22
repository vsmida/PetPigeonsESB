using System;
using System.Threading;

namespace Shared
{
    public class BackgroundThread
    {
        private readonly Thread _thread;
        public bool HasBeenStarted { get; set; }

        public BackgroundThread(Action threadWork)
        {
            _thread = new Thread(() =>
                                     {
                                         try
                                         {
                                             threadWork();
                                         }
                                         catch(Exception e)
                                         {
                                             Console.WriteLine(string.Format("Exception {0}", e.Message));
                                         }
                                     }) { IsBackground = true};   
        }

        public void Start()
        {
            _thread.Start();
            HasBeenStarted = true;
        }

        public void Join()
        {
            _thread.Join();
        }
    }
}
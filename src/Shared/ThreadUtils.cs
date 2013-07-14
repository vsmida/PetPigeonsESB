using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Shared
{
    public class ThreadUtils
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        public static ProcessThread CurrentProcessThread
        {
            get
            {
                uint currentThreadId = GetCurrentThreadId();
                foreach (ProcessThread processThread in Process.GetCurrentProcess().Threads)
                {
                    if (processThread.Id == currentThreadId)
                        return processThread;
                }
                throw new InvalidOperationException(
                    string.Format("Could not retrieve native thread with ID: {0}, current managed thread ID was {1}",
                                  currentThreadId,
                                  Thread.CurrentThread.ManagedThreadId));
            }
        }
    }
}
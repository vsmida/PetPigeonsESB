using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shared
{
    public sealed class RoundRobinThreadAffinedTaskScheduler : TaskScheduler, IDisposable
    {
        private BlockingCollection<Task> _tasks;
        private List<Thread> _threads;

        public RoundRobinThreadAffinedTaskScheduler(int numberOfThreads)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException("numberOfThreads");
            int[] processorIndexes = Enumerable.Range(0, Environment.ProcessorCount).ToArray();
            CreateThreads(numberOfThreads, processorIndexes);
        }

        public RoundRobinThreadAffinedTaskScheduler(int numberOfThreads, params int[] processorIndexes)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException("numberOfThreads");
            foreach (int num in processorIndexes)
            {
                if (num >= Environment.ProcessorCount || num < 0)
                    throw new ArgumentOutOfRangeException("processorIndexes",
                                                          string.Format(
                                                              "processor index {0} was supperior to the total number of processors in the system",
                                                              num));
            }
            CreateThreads(numberOfThreads, processorIndexes);
        }

        public override int MaximumConcurrencyLevel
        {
            get { return _threads.Count; }
        }



        public void Dispose()
        {
            if (_tasks == null)
                return;
            _tasks.CompleteAdding();
            _threads.ForEach((t => t.Join()));
            _tasks.Dispose();
            _tasks = null;
        }

        private void CreateThreads(int numberOfThreads, int[] processorIndexes)
        {
            _tasks = new BlockingCollection<Task>();
            _threads =
                Enumerable.Range(0, numberOfThreads).Select(
                    (i => new Thread((() => ThreadStartWithAffinity(i, processorIndexes)))
                              {
                                  IsBackground = true
                              })).ToList();
            _threads.ForEach((t => t.Start()));
        }

        private void ThreadStartWithAffinity(int threadIndex, int[] processorIndexes)
        {
            SetThreadAffinity(processorIndexes[threadIndex%processorIndexes.Length]);
            try
            {
                foreach (Task task in _tasks.GetConsumingEnumerable())
                    TryExecuteTask(task);
            }
            finally
            {
                RemoveThreadAffinity();
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }



        private static void SetThreadAffinity(int processorIndex)
        {
            Thread.BeginThreadAffinity();
            ThreadUtils.CurrentProcessThread.ProcessorAffinity = new IntPtr(1 << processorIndex);
        }

        private static void RemoveThreadAffinity()
        {
            ThreadUtils.CurrentProcessThread.ProcessorAffinity = new IntPtr((1 << Environment.ProcessorCount) - 1);
            Thread.EndThreadAffinity();
        }
    }
}
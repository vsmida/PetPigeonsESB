using System;
using System.Threading;

namespace ZmqServiceBus.Bus
{
    public interface ICompletionCallback : IBlockableUntilCompletion
    {
        void RegisterCallback(Action<AcknowledgementMessage> onCompletion);
        void WaitForCompletion();
        void ExecuteCallback(AcknowledgementMessage message);
    }

    public interface IBlockableUntilCompletion
    {
        void WaitForCompletion();        
    }

    public class DefaultCompletionCallback : ICompletionCallback, IBlockableUntilCompletion
    {
        private event Action<AcknowledgementMessage> _callbacks = delegate { };
        private AutoResetEvent _waitForCompletionHandle = new AutoResetEvent(false);
        public void RegisterCallback(Action<AcknowledgementMessage> onCompletion)
        {
            _callbacks += onCompletion;
        }

        public void WaitForCompletion()
        {
            _waitForCompletionHandle.WaitOne();
        }

        public void ExecuteCallback(AcknowledgementMessage message)
        {
            if (message.ProcessingSuccessful == false)
                throw new FailedMessageProcessingException(message);
            _callbacks(message);
            _waitForCompletionHandle.Set();
        }
    }
}
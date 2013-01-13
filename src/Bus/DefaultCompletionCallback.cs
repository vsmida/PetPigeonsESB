using System;
using System.Threading;
using Bus.InfrastructureMessages;

namespace Bus
{
    public class DefaultCompletionCallback : ICompletionCallback
    {
        private event Action<CompletionAcknowledgementMessage> _callbacks = delegate { };
        private AutoResetEvent _waitForCompletionHandle = new AutoResetEvent(false);
        public void RegisterCallback(Action<CompletionAcknowledgementMessage> onCompletion)
        {
            _callbacks += onCompletion;
        }

        public void WaitForCompletion(TimeSpan timeout)
        {
            _waitForCompletionHandle.WaitOne(timeout);
        }

        public void WaitForCompletion()
        {
            _waitForCompletionHandle.WaitOne();
        }

        public void ExecuteCallback(CompletionAcknowledgementMessage message)
        {
            if (message.ProcessingSuccessful == false)
                throw new FailedMessageProcessingException(message);
            _callbacks(message);
            _waitForCompletionHandle.Set();
        }
    }
}
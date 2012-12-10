using System;
using System.Threading;
using ZmqServiceBus.Bus.InfrastructureMessages;

namespace ZmqServiceBus.Bus
{
    public interface ICompletionCallback : IBlockableUntilCompletion
    {
        void RegisterCallback(Action<CompletionAcknowledgementMessage> onCompletion);
        void ExecuteCallback(CompletionAcknowledgementMessage message);
    }

    public interface IBlockableUntilCompletion
    {
        void WaitForCompletion();        
    }

    public class DefaultCompletionCallback : MarshalByRefObject, ICompletionCallback
    {
        private event Action<CompletionAcknowledgementMessage> _callbacks = delegate { };
        private AutoResetEvent _waitForCompletionHandle = new AutoResetEvent(false);
        public void RegisterCallback(Action<CompletionAcknowledgementMessage> onCompletion)
        {
            _callbacks += onCompletion;
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
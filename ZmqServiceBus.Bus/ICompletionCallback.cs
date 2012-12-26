using System;
using System.Threading;
using ZmqServiceBus.Bus.InfrastructureMessages;

namespace ZmqServiceBus.Bus
{
    public interface ICompletionCallback : IBlockableUntilCompletion, IBlockableUntilMessageReliablySent
    {
        void RegisterCallback(Action<CompletionAcknowledgementMessage> onCompletion);
        void ExecuteCallback(CompletionAcknowledgementMessage message);
    }

    public interface IBlockableUntilCompletion
    {
        void WaitForCompletion();        
    }

    public interface IBlockableUntilMessageReliablySent
    {
        void WaitForMessageToBeReliablySent();
        void Release();
    }

    public class DefaultCompletionCallback : ICompletionCallback
    {
        private event Action<CompletionAcknowledgementMessage> _callbacks = delegate { };
        private AutoResetEvent _waitForCompletionHandle = new AutoResetEvent(false);
        private AutoResetEvent _waitForMessageSendHandle = new AutoResetEvent(false);
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

        public void WaitForMessageToBeReliablySent()
        {
            _waitForMessageSendHandle.WaitOne();
        }

        public void Release()
        {
            _waitForMessageSendHandle.Set();
        }
    }
}
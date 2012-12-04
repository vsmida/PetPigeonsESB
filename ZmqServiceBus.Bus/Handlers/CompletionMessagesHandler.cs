﻿namespace ZmqServiceBus.Bus.Handlers
{
    public class CompletionMessagesHandler : ICommandHandler<CompletionAcknowledgementMessage>
    {
        private readonly ICallbackRepository _callbackRepository;

        public CompletionMessagesHandler(ICallbackRepository callbackRepository)
        {
            _callbackRepository = callbackRepository;
        }

        public void Handle(CompletionAcknowledgementMessage item)
        {
            var callback = _callbackRepository.GetCallback(item.MessageId);
            if (callback == null)
                return;

            callback.ExecuteCallback(item);

            _callbackRepository.RemoveCallback(item.MessageId);
        }
    }
}
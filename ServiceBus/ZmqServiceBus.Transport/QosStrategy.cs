namespace ZmqServiceBus.Transport
{
    public static class QosStrategy
    {
        private static readonly IQosStrategy _fireAndForget = new FireAndForgetStrategy();

        public static IQosStrategy FireAndForget { get { return _fireAndForget; } }
    }
}
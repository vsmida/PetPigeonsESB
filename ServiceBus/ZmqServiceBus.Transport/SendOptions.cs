namespace ZmqServiceBus.Transport
{
    public enum SendOptions
    {
        FireAndForget = 1,
        SomeoneAckedMessageReceived =2,
        ClientAndBrokerAckedMessageReceived,
    }
}
namespace ZmqServiceBus.Transport
{
    public enum ReliabilityOption
    {
        FireAndForget = 1,
        SendToClientAndBrokerNoAck = 2,
        SomeoneReceivedMessageOnTransport =3,
        ClientAndBrokerReceivedOnTransport = 4,
    }
}
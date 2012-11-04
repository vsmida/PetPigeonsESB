namespace Shared
{
    public enum ReliabilityLevel
    {
        FireAndForget = 1,
        SendToClientAndBrokerNoAck = 2,
        SomeoneReceivedMessageOnTransport =3,
        ClientAndBrokerReceivedOnTransport = 4,
    }
}
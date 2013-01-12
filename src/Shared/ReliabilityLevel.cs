namespace Shared
{
    public enum ReliabilityLevel
    {
        FireAndForget = 0,
        Ordered = 1, //sequence number
        Persisted = 2, //sequence number + broker when disconnected
    }
}
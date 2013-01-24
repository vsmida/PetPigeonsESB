namespace Bus.Transport.Network
{
    public class EndpointStatus
    {
        public readonly IEndpoint Endpoint;
        public bool Connected { get; set; }

        public EndpointStatus(IEndpoint endpoint, bool connected)
        {
            Connected = connected;
            Endpoint = endpoint;
        }
    }
}
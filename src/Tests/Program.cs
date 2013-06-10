using Tests.Integration;
using Tests.Integration.Performance;

namespace Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var perfTests = new PerformanceTests();
            var transportTest = new Transports();
           transportTest.zmq_transport_test();
           //  perfTests.should_send_messages();
        }
    }
}
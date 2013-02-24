using Tests.Integration;

namespace Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var perfTests = new PerformanceTests();
            perfTests.should_send_messages();
        }
    }
}
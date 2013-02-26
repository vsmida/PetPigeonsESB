using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgmTransportTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var t = new SenderReceiverIntegrationTests();
            t.tcp();
        }
    }
}

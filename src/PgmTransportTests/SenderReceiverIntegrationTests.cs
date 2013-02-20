using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PgmTransport;

namespace PgmTransportTests
{
    [TestFixture]
    public class SenderReceiverIntegrationTests
    {
        [Test]
        public void should_send_and_receive_a_message()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("Log4net.config"));
            var waitForMessage = new AutoResetEvent(false);
            var sender = new PgmSender();
            var receiver = new PgmReceiver();

            var ipEndPoint = new IPEndPoint(IPAddress.Parse("224.0.0.1"), 2000);
            receiver.ListenToEndpoint(ipEndPoint);
            receiver.OnMessageReceived += stream => OnMessageReceived(stream, waitForMessage);

            var buffer = Encoding.ASCII.GetBytes("Hello world");
            sender.Send(ipEndPoint, buffer);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 10000; i++)
            {
                sender.SendAsync(ipEndPoint, buffer);
                //sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("Hello world " + i));
                
            }
            sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("stop"));
         
                waitForMessage.WaitOne();
                
            watch.Stop();

            Console.WriteLine(string.Format("elapsed for async sends and receiving = : {0} ms", watch.ElapsedMilliseconds));


        }

        private void OnMessageReceived(Stream stream, AutoResetEvent waitForMessage)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);

            var message = Encoding.ASCII.GetString(buffer);
          //  Console.WriteLine(message);
            if(message == "stop")
            waitForMessage.Set();
        }
    }
}

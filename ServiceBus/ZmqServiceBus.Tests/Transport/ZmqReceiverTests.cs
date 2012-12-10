using System;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Tests.Transport
{
    public class ZmqReceiverTests
    {
        private DataReceiver _receiver;
        private ZmqContext _context;
        private FakeTransportConfiguration _configuration = new FakeTransportConfiguration();

        [SetUp]
        public void setup()
        {
            _context = ZmqContext.Create();
            _receiver = new DataReceiver(_context, _configuration);
        }


        [Test, Timeout(1000), Repeat(3)]
        public void should_create_command_receiving_socket()
        {
            _receiver.Initialize();
            AutoResetEvent waitForMessage = new AutoResetEvent(false);
            var peerName = "peer";
            var id = Guid.NewGuid();
            var type = "type";
            var message = new byte[2];

            _receiver.OnMessageReceived += mess =>
                                               {
                                                   Assert.AreEqual(type,mess.MessageType);
                                                   Assert.AreEqual(peerName,mess.PeerName);
                                                   Assert.AreEqual(id,mess.MessageIdentity);
                                                   Assert.AreEqual(message,mess.Data);
                                                   
                                                   waitForMessage.Set();
                                               };
            var pubSocket = _context.CreateSocket(SocketType.PUB);
            pubSocket.Connect(_configuration.GetCommandsConnectEnpoint());

            pubSocket.SendMore(type, Encoding.ASCII);
            pubSocket.SendMore(peerName, Encoding.ASCII);
            pubSocket.SendMore(id.ToByteArray());
            pubSocket.Send(message);

            pubSocket.Dispose();
        }


        [TearDown]
        public void teardown()
        {
            _receiver.Dispose();
            _context.Dispose();
        }
    }
}
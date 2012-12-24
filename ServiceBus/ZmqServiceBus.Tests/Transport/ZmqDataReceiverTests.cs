using System;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.Network;
using Shared;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class ZmqDataReceiverTests
    {
        private ZmqDataReceiver _receiver;
        private ZmqContext _context;
        private FakeTransportConfiguration _configuration;

        [SetUp]
        public void setup()
        {
            _configuration = new FakeTransportConfiguration();
            _context = ZmqContext.Create();
            _receiver = new ZmqDataReceiver(_context, _configuration);
        }


        [Test, Timeout(100000000), Repeat(3)]
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
            var pushsocket = _context.CreateSocket(SocketType.PUSH);
            var commandsConnectEnpoint = _configuration.GetCommandsConnectEnpoint();
            pushsocket.Connect(commandsConnectEnpoint);
            
            pushsocket.SendMore(type, Encoding.ASCII);
            pushsocket.SendMore(peerName, Encoding.ASCII);
            pushsocket.SendMore(id.ToByteArray());
            pushsocket.Send(message);
            waitForMessage.WaitOne();
            pushsocket.Dispose();
        }


        [TearDown]
        public void teardown()
        {
            _receiver.Dispose();
            _context.Dispose();
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using Bus;
using Bus.Dispatch;
using Bus.Serializer;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using NUnit.Framework;
using Shared;
using Tests.Transport;

namespace Tests.Integration.Performance
{
    [TestFixture]
    public class Serialization
    {
        private MessageWireData _testData = new MessageWireData(typeof(TestData.FakeCommand).FullName,
                                                                Guid.NewGuid(), "tt", new byte[10]);

        [Test]
        public void serializationTestsProto()
        {
            using (var perfMeasure = new PerformanceMeasure(() =>
                                                   {
                                                       var ser = BusSerializer.Serialize(_testData);
                                                       BusSerializer.Deserialize<MessageWireData>(ser);

                                                   }, 1000000)) ;
        }

        [Test]
        public void serializationTestsCustom()
        {
            var serializer = new MessageWireDataSerializer(new AssemblyScanner());
            using (var perfMeasure = new PerformanceMeasure(() =>
                                                               {
                                                                   var ser = serializer.Serialize(_testData);
                                                                   using (var stream = new MemoryStream(ser))
                                                                       serializer.Deserialize(stream);

                                                               }, 1000000)) ;
        }
    }
}
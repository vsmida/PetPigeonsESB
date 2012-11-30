using DirectoryService.Core;
using NUnit.Framework;

namespace DirectoryService.Event
{
    [TestFixture]
    public class ServiceStoreTests
    {
        private ServiceStore _store;

        [SetUp]
        public void setup()
        {
            _store = new ServiceStore();
        }

        //[Test]
        //public void should_store_command_handler_endpoint_in_memory()
        //{
        //    string endpoint = "endpoint";
        //    _store.RegisterCommandHandler<RegisterCommandHandlerCommand>(endpoint);
        //    _store.RegisterCommandHandler<RegisterCommandHandlerCommand>(endpoint);
        //    Assert.AreEqual(_store.GetCommandEndpoints<RegisterCommandHandlerCommand>().FirstOrDefault(), endpoint);
        //}

        //[Test]
        //public void should_store_event_publishers_endpoint_in_memory()
        //{
        //    string endpoint = "endpoint";
        //    _store.RegisterEventPublisher<RegisteredHandlersForCommand>(endpoint);
        //    _store.RegisterCommandHandler<RegisterCommandHandlerCommand>(endpoint);
        //    Assert.AreEqual(_store.GetEventEndpoints<RegisteredHandlersForCommand>().FirstOrDefault(), endpoint);
        //}

        //[Test]
        //public void should__not_store_event_publishers_endpoint_twice()
        //{
        //    string endpoint = "endpoint";
        //    _store.RegisterEventPublisher<RegisteredHandlersForCommand>(endpoint);
        //    _store.RegisterEventPublisher<RegisteredHandlersForCommand>(endpoint);
        //    var eventEndpoints = _store.GetEventEndpoints<RegisteredHandlersForCommand>().ToArray();
        //    Assert.AreEqual(eventEndpoints.Count(), 1);
        //    Assert.AreEqual(eventEndpoints.FirstOrDefault(), endpoint);
        //}

        //[Test]
        //public void should_not_store_command_handler_endpoints_twice()
        //{
        //    string endpoint = "endpoint";
        //    _store.RegisterCommandHandler<RegisterCommandHandlerCommand>(endpoint);
        //    _store.RegisterCommandHandler<RegisterCommandHandlerCommand>(endpoint);
        //    var commandEndpoints = _store.GetCommandEndpoints<RegisterCommandHandlerCommand>().ToArray();
        //    Assert.AreEqual(commandEndpoints.Count(), 1);
        //    Assert.AreEqual(commandEndpoints.FirstOrDefault(), endpoint);
        //}

    }
}
using DirectoryService.Core;
using Moq;
using NUnit.Framework;

namespace DirectoryService.Tests
{
    [TestFixture]
    public class RegisterCommandHandlerTests
    {
        private Mock<IServiceStore> _serviceStoreMock;
        private RegisterServiceCommandHandler _handler;

        [SetUp]
        public void setup()
        {
            _serviceStoreMock = new Mock<IServiceStore>();
            _handler = new RegisterServiceCommandHandler();
        }

    }
}
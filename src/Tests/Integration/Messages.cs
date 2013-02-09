using System;
using Bus.Attributes;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using ProtoBuf;
using Shared;

namespace Tests.Integration
{
    [ProtoContract]
    public class FakeNumberCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly int Number;

        public FakeNumberCommand(int number)
        {
            Number = number;
        }

        private FakeNumberCommand()
        {

        }

    }


    [ProtoContract]
    [BusOptions(ReliabilityLevel.Persisted, WireTransportType.ZmqPushPullTransport)]
    public class FakePersistingCommand : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly int Number;

        public FakePersistingCommand(int number)
        {
            Number = number;
        }

        private FakePersistingCommand()
        {
        }

    }



    public class FakePersistingCommandHandler : ICommandHandler<FakePersistingCommand>
    {
        public static event Action<int> OnCommandReceived = delegate { };

        public void Handle(FakePersistingCommand item)
        {
            OnCommandReceived(item.Number);
        }

    }

    public class FakeCommandHandler : ICommandHandler<FakeNumberCommand>
    {
        public static event Action<int> OnCommandReceived = delegate { };

        public void Handle(FakeNumberCommand item)
        {
            OnCommandReceived(item.Number);
        }

    }
}
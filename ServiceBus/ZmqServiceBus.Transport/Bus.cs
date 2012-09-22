using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ProtoBuf;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public class Bus : IBus
    {
        private ZmqContext _context;
        private Dictionary<string, BlockingCollection<ICommand>> _endpointsToCommandQueue = new Dictionary<string, BlockingCollection<ICommand>>();
        private Dictionary<Type, string> _commandTypesToEndpoints = new Dictionary<Type, string>();
        private event Action OnDispose;

        public void Initialize(ITransportConfiguration config)
        {
            _context = ZmqContext.Create();

        }

        public void RegisterEventPublisher<T>(string endpoint) where T : IEvent
        {
            throw new System.NotImplementedException();
        }

        public void RegisterCommandHandler<T>(string endpoint) where T : ICommand
        {
           }

      

        public void SendCommand<T>(T command) where T : ICommand
        {
            _endpointsToCommandQueue[_commandTypesToEndpoints[typeof(T)]].Add(command);
        }

        public void PublishEvent<T>(T message) where T : IEvent
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            OnDispose();
            _context.Terminate();
        }
    }
}
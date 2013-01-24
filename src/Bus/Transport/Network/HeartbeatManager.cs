using System;
using System.Collections.Concurrent;
using System.Threading;
using Bus.BusEventProcessorCommands;
using Bus.InfrastructureMessages.Heartbeating;
using Bus.Transport.SendingPipe;

namespace Bus.Transport.Network
{
    public interface IHeartbeatManager : IDisposable
    {
        event Action<IEndpoint> Disconnected;
        event Action<IEndpoint> Reconnected;
        void StartMonitoring(IEndpoint endpoint);
        void RegisterHeartbeat(HeartbeatMessage heartbeat);
        void Initialize();
    }

    public class HeartbeatManager : IHeartbeatManager
    {
        private class HeartbeatInformation
        {
            public DateTime? LastHeartbeat;
            public bool? IsConnected;
            public DateTime? LastSentHeartbeat;
        }

        private readonly ConcurrentDictionary<IEndpoint, HeartbeatInformation> _heartbeatsByEndpoint = new ConcurrentDictionary<IEndpoint, HeartbeatInformation>();
        private readonly IHeartbeatingConfiguration _heartbeatingConfiguration;
        private Timer _timer;
        private IMessageSender _messageSender;
        private IDataReceiver _dataReceiver;

        public HeartbeatManager(IHeartbeatingConfiguration heartbeatingConfiguration, IMessageSender messageSender, IDataReceiver dataReceiver)
        {
            _heartbeatingConfiguration = heartbeatingConfiguration;
            _messageSender = messageSender;
            _dataReceiver = dataReceiver;
        }

        public event Action<IEndpoint> Disconnected = delegate { };
        public event Action<IEndpoint> Reconnected = delegate { };


        public void StartMonitoring(IEndpoint endpoint)
        {
            _heartbeatsByEndpoint.AddOrUpdate(endpoint, new HeartbeatInformation(), (point, oldinfo) => oldinfo);
        }

        public void RegisterHeartbeat(HeartbeatMessage heartbeat)
        {
            if ((DateTime.UtcNow - heartbeat.TimestampUtc) > _heartbeatingConfiguration.HeartbeatInterval)
                return;
            HeartbeatInformation info;
            if (!_heartbeatsByEndpoint.TryGetValue(heartbeat.Endpoint, out info))
            {
                info = new HeartbeatInformation();
                _heartbeatsByEndpoint.TryAdd(heartbeat.Endpoint, info);
            }
            if (info.IsConnected == false)
            {
                Reconnected(heartbeat.Endpoint);
            }
            info.LastHeartbeat = heartbeat.TimestampUtc;
            info.IsConnected = true;
        }

        public void Initialize()
        {
            _timer = new Timer(s =>
                                   {
                                       foreach (var endpointToInfo in _heartbeatsByEndpoint.ToArray())
                                       {
                                           if (endpointToInfo.Value.LastSentHeartbeat != null && endpointToInfo.Value.IsConnected == true)
                                               if ((DateTime.UtcNow - endpointToInfo.Value.LastHeartbeat) > _heartbeatingConfiguration.HeartbeatInterval)
                                               {
                                                   Disconnected(endpointToInfo.Key);
                                                   endpointToInfo.Value.IsConnected = false;
                                                   _messageSender.InjectNetworkSenderCommand(new DisconnectEndpoint(endpointToInfo.Key));
                                               }

                                           _messageSender.SendHeartbeat(endpointToInfo.Key);
                                           endpointToInfo.Value.LastSentHeartbeat = DateTime.UtcNow;
                                       }
                                   }, null, 0, (int)_heartbeatingConfiguration.HeartbeatInterval.TotalMilliseconds / 2);
        }

        public void Dispose()
        {
            var waitForDispose = new AutoResetEvent(false);
            _timer.Dispose(waitForDispose);
            waitForDispose.WaitOne();
        }
    }

    public interface IHeartbeatingConfiguration
    {
        TimeSpan HeartbeatInterval { get; }
    }

    class DummyHeartbeatingConfig : IHeartbeatingConfiguration
    {
        private TimeSpan _timeout = TimeSpan.FromSeconds(2);
        public TimeSpan HeartbeatInterval { get { return _timeout; } set { _timeout = value; } }
    }
}
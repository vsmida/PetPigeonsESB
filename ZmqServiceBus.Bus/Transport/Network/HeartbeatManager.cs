using System;
using System.Collections.Concurrent;
using System.Threading;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IHeartbeatManager : IDisposable
    {
        event Action<IEndpoint> Disconnected;
        event Action<IEndpoint> Reconnected;
        void StartMonitoring(IEndpoint endpoint);
        void RegisterHeartbeat(HeartbeatMessage heartbeat);
        void Initialize();
        event Action<IEndpoint> CheckPeerHeartbeat;
    }

    public class HeartbeatManager : IHeartbeatManager
    {
        private class HeartbeatInformation
        {
            public DateTime? LastHeartbeat;
            public bool? IsConnected;
        }

        private readonly ConcurrentDictionary<IEndpoint, HeartbeatInformation> _heartbeatsByEndpoint = new ConcurrentDictionary<IEndpoint, HeartbeatInformation>();
        private readonly IHeartbeatingConfiguration _heartbeatingConfiguration;
        private Timer _timer;
        public event Action<IEndpoint> CheckPeerHeartbeat = delegate { };

        public HeartbeatManager(IHeartbeatingConfiguration heartbeatingConfiguration)
        {
            _heartbeatingConfiguration = heartbeatingConfiguration;
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
            if (info.IsConnected != true)
                Reconnected(heartbeat.Endpoint);
            info.LastHeartbeat = heartbeat.TimestampUtc;
            info.IsConnected = true;
        }

        public void Initialize()
        {
            _timer = new Timer(s =>
                                   {
                                       foreach (var endpointToInfo in _heartbeatsByEndpoint.ToArray())
                                       {
                                           if (endpointToInfo.Value.LastHeartbeat != null)
                                               if ((DateTime.UtcNow - endpointToInfo.Value.LastHeartbeat) > _heartbeatingConfiguration.HeartbeatInterval)
                                               {
                                                   Disconnected(endpointToInfo.Key);
                                                   endpointToInfo.Value.IsConnected = false;
                                               }
                                           CheckPeerHeartbeat(endpointToInfo.Key);
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
        public TimeSpan HeartbeatInterval { get { return TimeSpan.FromSeconds(2); }}
    }
}
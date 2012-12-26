using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Shared;
using ZeroMQ;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using System.Linq;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IDataSender : IDisposable
    {
        void Initialize();
        void SendMessage(ISendingBusMessage message);
    }

    public class DataSender : IDataSender
    {
        private Dictionary<WireSendingTransportType, IWireSendingTransport> _wireSendingTransports;
        private readonly IHeartbeatManager _heartbeatManager;
        private Thread _syncThread;
        private readonly BlockingCollection<ISendingBusMessage> _messagesToSend = new BlockingCollection<ISendingBusMessage>();
        private  volatile bool _running = true;


        public DataSender(IWireSendingTransport[] wireSendingTransports, IHeartbeatManager heartbeatManager)
        {
            _heartbeatManager = heartbeatManager;
            _wireSendingTransports = wireSendingTransports.ToDictionary(x => x.TransportType, x => x);
            CreateDequeueThread();
            _heartbeatManager.CheckPeerHeartbeat += OnHeatbeatRequested;

        }

        private void OnHeatbeatRequested(ISendingBusMessage heartbeat)
        {
            SendMessageInternal(heartbeat);
        }

        private void CreateDequeueThread()
        {
            _syncThread = new Thread(() =>
                                         {
                                             ISendingBusMessage sendingMessage;
                                             while(_running)
                                             {
                                                 if(_messagesToSend.TryTake(out sendingMessage, 300))
                                                     SendMessageInternal(sendingMessage);
                                             }
                                         });
            _syncThread.Start();
        }

        public void Dispose()
        {
            _running = false;
            foreach (var wireSendingTransport in _wireSendingTransports.Values)
            {
                wireSendingTransport.Dispose();
            }
            _syncThread.Join();
        }

        public void Initialize()
        {
            foreach (var wireSendingTransport in _wireSendingTransports.Values)
            {
                wireSendingTransport.Initialize();
            }
        }

        public void SendMessage(ISendingBusMessage message)
        {
            _messagesToSend.Add(message);
        }

        private void SendMessageInternal(ISendingBusMessage message)
        {
            foreach (var endpoint in message.TargetEndpoints)
            {
                _heartbeatManager.StartMonitoring(endpoint);
                _wireSendingTransports[endpoint.WireTransportType].SendMessage(message, endpoint);
            }
        }
    }
}
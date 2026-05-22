using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NOBlackBox
{
    internal class RTTTelemetryServer : IDisposable
    {
        private readonly int _port;
        private readonly string _bindAddress;
        private readonly int _maxClients;
        private readonly int _clientQueueLimit;
        private readonly string _password;
        private readonly ManualLogSource? _logger;

        private TcpListener? _listener;
        private CancellationTokenSource _cts;
        private ConcurrentDictionary<Guid, RTTClient> _clients = new();
        private List<string> _replayPrefix = new();
        private readonly object _sessionLock = new();
        private int _activeConnections;

        internal RTTTelemetryServer(
            int port,
            string bindAddress,
            int maxClients,
            int clientQueueLimit,
            string password,
            ManualLogSource? logger)
        {
            _port = port;
            _bindAddress = bindAddress;
            _maxClients = maxClients;
            _clientQueueLimit = clientQueueLimit;
            _password = password;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        internal void Start()
        {
            var addr = string.IsNullOrEmpty(_bindAddress) || _bindAddress == "0.0.0.0"
                ? IPAddress.Any
                : IPAddress.Parse(_bindAddress);

            _listener = new TcpListener(addr, _port);
            _listener.Start();
            _ = Task.Run(() => AcceptLoop(_cts.Token));
        }

        internal void Stop()
        {
            _cts.Cancel();
            try { _listener?.Stop(); } catch { }
        }

        public void Dispose()
        {
            Stop();

            foreach (var kvp in _clients)
            {
                kvp.Value.RequestDisconnect("server shutdown");
            }

            _clients.Clear();
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient tcpClient;
                    try
                    {
                        tcpClient = await _listener!.AcceptTcpClientAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"RTT accept error: {ex.Message}");
                        continue;
                    }

                    _ = HandleClientAsync(tcpClient);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            string endpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";

            if (Interlocked.Increment(ref _activeConnections) > _maxClients)
            {
                Interlocked.Decrement(ref _activeConnections);
                _logger?.LogWarning($"RTT max clients ({_maxClients}) reached, rejecting {endpoint}");
                tcpClient.Close();
                return;
            }

            var client = new RTTClient(
                tcpClient,
                this,
                Guid.NewGuid(),
                _clientQueueLimit,
                _password,
                _logger);

            try
            {
                bool handshakeOk = await client.PerformHandshakeAsync();
                if (!handshakeOk)
                {
                    Interlocked.Decrement(ref _activeConnections);
                    tcpClient.Close();
                    return;
                }

                List<string> prefixSnapshot;
                lock (_sessionLock)
                {
                    prefixSnapshot = new List<string>(_replayPrefix);
                    _clients.TryAdd(client.Id, client);
                }

                client.StartWriter(prefixSnapshot);

                _logger?.LogInfo($"RTT client registered: {endpoint}");
            }
            catch (Exception ex)
            {
                Interlocked.Decrement(ref _activeConnections);
                _logger?.LogWarning($"RTT client setup error: {endpoint}: {ex.Message}");
                tcpClient.Close();
            }
        }

        internal void RemoveClient(Guid id)
        {
            if (_clients.TryRemove(id, out _))
            {
                Interlocked.Decrement(ref _activeConnections);
            }
        }

        internal void BeginSession()
        {
            lock (_sessionLock)
            {
                _replayPrefix.Clear();
            }
        }

        internal void PublishLine(string line, bool replay)
        {
            var msg = new RttMessage(line, false);

            if (replay)
            {
                lock (_sessionLock)
                {
                    _replayPrefix.Add(line);
                }
            }

            foreach (var kvp in _clients)
            {
                kvp.Value.TryEnqueue(msg);
            }
        }

        internal void PublishFlushMarker()
        {
            foreach (var kvp in _clients)
            {
                kvp.Value.TryEnqueue(RttMessage.FlushMessage);
            }
        }

        internal void EndSession(bool disconnectClients)
        {
            List<RTTClient> snapshot;
            lock (_sessionLock)
            {
                snapshot = new List<RTTClient>(_clients.Values);
                _replayPrefix.Clear();
            }

            if (disconnectClients)
            {
                foreach (var client in snapshot)
                {
                    client.RequestDisconnect("session end");
                }
            }
        }
    }
}

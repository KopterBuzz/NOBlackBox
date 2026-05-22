using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NOBlackBox
{
    internal class RttMessage
{
    internal static readonly RttMessage FlushMessage = new(null, true);

    public string? Line { get; }
    public bool Flush { get; }

    public RttMessage(string? line, bool flush)
    {
        Line = line;
        Flush = flush;
    }
}

    internal class RTTClient
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly string _endpoint;
        private readonly RTTTelemetryServer _server;
        private readonly Guid _id;
        private readonly int _queueLimit;
        private readonly int _handshakeTimeoutMs;
        private readonly int _maxHandshakeBytes;
        private readonly string _password;
        private readonly ManualLogSource? _logger;

        private readonly ConcurrentQueue<RttMessage> _queue = new();
        private int _queuedCount;
        private int _disconnectRequested;
        private CancellationTokenSource _cts;
        private Task? _writerTask;

        internal Guid Id => _id;
        internal string Endpoint => _endpoint;

        internal RTTClient(
            TcpClient tcpClient,
            RTTTelemetryServer server,
            Guid id,
            int queueLimit,
            string password,
            ManualLogSource? logger)
        {
            _tcpClient = tcpClient;
            _tcpClient.NoDelay = true;
            _stream = tcpClient.GetStream();
            _endpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
            _server = server;
            _id = id;
            _queueLimit = queueLimit;
            _handshakeTimeoutMs = 5000;
            _maxHandshakeBytes = 4096;
            _password = password;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        internal async Task<bool> PerformHandshakeAsync()
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(_handshakeTimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, _cts.Token);
                var ct = linkedCts.Token;

                // Host sends first: protocol greeting
                byte[] hostGreeting = Encoding.UTF8.GetBytes(
                    "XtraLib.Stream.0\nTacview.RealTimeTelemetry.0\nNOBlackBox\n\0");
                await _stream.WriteAsync(hostGreeting, 0, hostGreeting.Length, ct);
                await _stream.FlushAsync(ct);

                // Client responds with protocol + username + password hash
                byte[] response = await ReadUntilNullAsync(_maxHandshakeBytes, ct);
                if (response == null)
                {
                    _logger?.LogWarning($"RTT handshake: {_endpoint} sent no response");
                    return false;
                }

                string[] fields = Encoding.UTF8.GetString(response).Split('\n');
                if (fields.Length != 4)
                {
                    _logger?.LogWarning(
                        $"RTT handshake: {_endpoint} sent {fields.Length} fields, expected 4");
                    return false;
                }

                if (fields[0] != "XtraLib.Stream.0" ||
                    fields[1] != "Tacview.RealTimeTelemetry.0")
                {
                    _logger?.LogWarning(
                        $"RTT handshake: {_endpoint} protocol mismatch " +
                        $"({fields[0]}, {fields[1]})");
                    return false;
                }

                string username = fields[2];
                foreach (char c in username)
                {
                    if (c <= 0x1F && c != '\t')
                    {
                        _logger?.LogWarning(
                            $"RTT handshake: {_endpoint} username contains " +
                            $"control character 0x{(int)c:X2}");
                        return false;
                    }
                }

                string clientHash = fields[3];

                if (!string.IsNullOrEmpty(_password))
                {
                    string expectedHash = Helpers.ComputePasswordHash(_password);
                    if (!string.Equals(clientHash, expectedHash,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogWarning(
                            $"RTT handshake: {_endpoint} password mismatch " +
                            $"(expected {expectedHash}, got {clientHash})");
                        return false;
                    }
                }

                _logger?.LogInfo(
                    $"RTT client connected: {username} ({_endpoint})");

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning(
                    $"RTT handshake: {_endpoint} timed out");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    $"RTT handshake: {_endpoint} error: {ex.Message}");
                return false;
            }
        }

        private async Task<byte[]?> ReadUntilNullAsync(int maxBytes, CancellationToken ct)
        {
            using var ms = new MemoryStream(maxBytes);
            byte[] buf = new byte[1];
            int total = 0;

            while (total < maxBytes)
            {
                int read = await _stream.ReadAsync(buf, 0, 1, ct);
                if (read == 0)
                    return null;

                total++;
                if (buf[0] == 0)
                    break;

                ms.WriteByte(buf[0]);
            }

            return ms.ToArray();
        }

        internal void StartWriter(List<string> replayPrefix)
        {
            foreach (var line in replayPrefix)
            {
                _queue.Enqueue(new RttMessage(line, false));
                Interlocked.Increment(ref _queuedCount);
            }

            _writerTask = Task.Run(() => WriterLoop(_cts.Token));
        }

        private async Task WriterLoop(CancellationToken ct)
        {
            try
            {
                var writer = new StreamWriter(
                    _stream,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    bufferSize: 8192,
                    leaveOpen: true
                )
                {
                    NewLine = "\n",
                    AutoFlush = false
                };

                while (!ct.IsCancellationRequested)
                {
                    if (_queue.TryDequeue(out var msg))
                    {
                        Interlocked.Decrement(ref _queuedCount);

                        if (msg.Flush)
                        {
                            await writer.FlushAsync();
                        }
                        else if (msg.Line != null)
                        {
                            await writer.WriteAsync(msg.Line);
                            await writer.WriteAsync('\n');
                        }
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(10, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }

                await writer.FlushAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    $"RTT writer: {_endpoint} error: {ex.Message}");
            }
            finally
            {
                CleanupSocket();
                _server.RemoveClient(_id);
            }
        }

        internal bool TryEnqueue(RttMessage msg)
        {
            if (_disconnectRequested != 0)
                return false;

            if (Interlocked.Increment(ref _queuedCount) > _queueLimit)
            {
                Interlocked.Decrement(ref _queuedCount);
                _logger?.LogWarning(
                    $"RTT client queue full ({_endpoint}), disconnecting");
                RequestDisconnect("queue overflow");
                return false;
            }

            _queue.Enqueue(msg);
            return true;
        }

        internal void RequestDisconnect(string reason)
        {
            if (Interlocked.Exchange(ref _disconnectRequested, 1) != 0)
                return;

            _ = Task.Run(() => DisconnectAsync(reason, graceful: true));
        }

        private async Task DisconnectAsync(string reason, bool graceful)
        {
            try
            {
                if (graceful)
                {
                    EnqueueBestEffort(new RttMessage(null, true));
                    await DrainForAsync(TimeSpan.FromSeconds(1));
                }

                _logger?.LogInfo(
                    $"RTT client disconnected: {_endpoint} ({reason})");
            }
            catch { }
            finally
            {
                _cts.Cancel();
                _server.RemoveClient(_id);
            }
        }

        private void EnqueueBestEffort(RttMessage msg)
        {
            Interlocked.Increment(ref _queuedCount);
            _queue.Enqueue(msg);
        }

        private async Task DrainForAsync(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline && !_queue.IsEmpty)
            {
                await Task.Delay(10);
            }
        }

        private void CleanupSocket()
        {
            try
            {
                _tcpClient.Close();
            }
            catch { }
        }

        internal async Task CleanShutdownAsync()
        {
            if (Interlocked.Exchange(ref _disconnectRequested, 1) != 0)
                return;

            EnqueueBestEffort(new RttMessage(null, true));
            await DrainForAsync(TimeSpan.FromSeconds(1));
            _cts.Cancel();
            if (_writerTask != null)
            {
                try { await _writerTask; }
                catch { }
            }
            CleanupSocket();
            _server.RemoveClient(_id);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NOBlackBox.src.Recorder
{
    internal static class Server
    {
        private static readonly Thread handler = new(Start)
        {
            IsBackground = true
        };

        private static CancellationTokenSource? source;

        private static bool enabled = false;
        public static bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                enabled = value;
                Interlocked.MemoryBarrier();

                if (!enabled)
                    source?.Cancel();
                else
                    if (!handler.IsAlive)
                        handler.Start();
            }
        }

        private static void Start()
        {
            TcpListener listener = new(IPAddress.Any, 42674);
            List<TcpClient> clients = [];

            source = new();

            listener.Start();

            try
            {
                while (enabled)
                {
                    var task = listener.AcceptTcpClientAsync();
                    task.Wait(source.Token);

                    TcpClient client = task.Result;
                    NetworkStream stream = client.GetStream();

                    stream.Write("XtraLib.Stream.0\r\nTacview.RealTimeTelemetry.0\r\nHost username\r\n\0\n"u8);
                }
            }
            catch
            {}

            source = null;

            foreach (var client in clients)
                client.Close();
        }
    }
}

using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NOBlackBox
{
    internal class MultiThreadedStreamWriter
    {
        private StreamWriter _writer;
        private object _stateLock = new object();
        private volatile bool _shouldFlush = false;
        private volatile bool _shouldClose = false;
        private ConcurrentQueue<string> _lineQueue;
        private Task _writeTask;

        public MultiThreadedStreamWriter(StreamWriter writer)
        {
            _writer = writer;
            _lineQueue = new ConcurrentQueue<string>();
            _writeTask = Task.Run(() => _writeLoop());
        }

        private void _writeLoop()
        {
            Plugin.Logger?.LogDebug("[NOBlackBox]: Starting ACMI StreamWriter Thread.");
            bool flushThisLoop = false;
            bool closeThisLoop = false;
            while (true)
            {
                lock (_stateLock)
                {
                    flushThisLoop = _shouldFlush;
                    closeThisLoop = _shouldClose;
                }

                if (closeThisLoop)
                {
                    while (!_lineQueue.IsEmpty)
                    {
                        //Empty the line queue
                        string newLine;
                        _lineQueue.TryDequeue(out newLine);
                        _writer.WriteLine(_lineQueue);
                    }
                    _writer.Flush();
                    _writer.Close();
                    Plugin.Logger?.LogDebug("[NOBlackBox]: Exiting ACMI StreamWriter Thread.");
                    return;
                }

                if (flushThisLoop)
                {
                    Plugin.Logger?.LogDebug("[NOBlackBox]: ACMI Flush started.");
                    _writer.Flush();
                    Plugin.Logger?.LogDebug("[NOBlackBox]: ACMI Flush complete.");
                    lock (_stateLock)
                    {
                        _shouldFlush = false;
                        flushThisLoop = false;
                    }
                }

                var linesWrittenThisLoop = 0;
                while (!_lineQueue.IsEmpty && linesWrittenThisLoop < 100)
                {
                    string newLine;
                    _lineQueue.TryDequeue(out newLine);
                    _writer.WriteLine(newLine);
                    linesWrittenThisLoop++;
                }
            }
        }

        public int PendingLineCount()
        {
            return _lineQueue.Count;
        }

        public void Flush()
        {
            Plugin.Logger?.LogDebug("[NOBlackBox]: ACMI Flush requested.");
            lock (_stateLock)
            {
                _shouldFlush = true;
            }
        }

        public void Close()
        {
            //Set the close flag and wait for the task to finish.
            lock (_stateLock)
            {
                _shouldClose = true;
            }
            _writeTask.Wait();
        }

        public void WriteLine(string line)
        {
            _lineQueue.Enqueue(line);
        }
    }
}

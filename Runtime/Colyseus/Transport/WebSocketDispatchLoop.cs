#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Threading;

namespace Colyseus
{
    internal static class WebSocketDispatchLoop
    {
        private const int PollIntervalMilliseconds = 15;

        private static readonly object SyncRoot = new object();
        private static readonly HashSet<WebSocketTransport> Sockets = new HashSet<WebSocketTransport>();
        private static readonly AutoResetEvent WakeUp = new AutoResetEvent(false);

        private static Thread _dispatchThread;

        public static void Register(WebSocketTransport socket)
        {
            if (socket == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (!Sockets.Add(socket))
                {
                    return;
                }

                EnsureThread();
            }

            WakeUp.Set();
        }

        public static void Unregister(WebSocketTransport socket)
        {
            if (socket == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                Sockets.Remove(socket);
            }

            WakeUp.Set();
        }

        private static void EnsureThread()
        {
            if (_dispatchThread != null)
            {
                return;
            }

            _dispatchThread = new Thread(DispatchLoop)
            {
                IsBackground = true,
                Name = "Colyseus.WebSocketDispatch"
            };
            _dispatchThread.Start();
        }

        private static void DispatchLoop()
        {
            while (true)
            {
                WebSocketTransport[] sockets;

                lock (SyncRoot)
                {
                    if (Sockets.Count == 0)
                    {
                        sockets = null;
                    }
                    else
                    {
                        sockets = new WebSocketTransport[Sockets.Count];
                        Sockets.CopyTo(sockets);
                    }
                }

                if (sockets == null)
                {
                    WakeUp.WaitOne();
                    continue;
                }

                for (var i = 0; i < sockets.Length; i++)
                {
                    try
                    {
                        sockets[i].DispatchMessageQueueFromSharedLoop();
                    }
                    catch (Exception e)
                    {
                        ColyseusContext.Logger?.LogError($"DispatchMessageQueue error: {e.Message}");
                    }
                }

                WakeUp.WaitOne(PollIntervalMilliseconds);
            }
        }
    }
}

#endif

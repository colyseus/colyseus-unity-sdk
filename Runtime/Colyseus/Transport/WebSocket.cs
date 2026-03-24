using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Colyseus
{
    public class WebSocketTransport
    {
        public event WebSocketOpenEventHandler OnOpen;
        public event WebSocketMessageEventHandler OnMessage;
        public event WebSocketErrorEventHandler OnError;
        public event WebSocketCloseEventHandler OnClose;

        private readonly object _dispatchLock = new object();
        private NativeWebSocket.WebSocket _ws;
        private bool _usesSharedDispatchLoop;

        internal static bool ShouldUseSharedDispatchLoop(SynchronizationContext synchronizationContext)
        {
            return ColyseusContext.RegisterWebSocketForDispatch == null && synchronizationContext == null;
        }

        public async Task Connect(string url, Dictionary<string, string> headers)
        {
            var synchronizationContext = SynchronizationContext.Current;
            var socket = new NativeWebSocket.WebSocket(url, headers);
            _ws = socket;
            _usesSharedDispatchLoop = false;

            socket.OnOpen += () => OnOpen?.Invoke();
            socket.OnMessage += (data) => OnMessage?.Invoke(data);
            socket.OnError += (msg) => OnError?.Invoke(msg);
            socket.OnClose += (code) =>
            {
                ColyseusContext.UnregisterWebSocketForDispatch?.Invoke(socket);
#if !UNITY_WEBGL || UNITY_EDITOR
                lock (_dispatchLock)
                {
                    _usesSharedDispatchLoop = false;
                }

                WebSocketDispatchLoop.Unregister(this);
#endif
                OnClose?.Invoke((int)code);
            };

            if (ColyseusContext.RegisterWebSocketForDispatch != null)
            {
                // External dispatcher (MonoGame, custom engine loop, etc.) handles DispatchMessageQueue
                ColyseusContext.RegisterWebSocketForDispatch(socket);
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            else if (ShouldUseSharedDispatchLoop(synchronizationContext))
            {
                _usesSharedDispatchLoop = true;
                WebSocketDispatchLoop.Register(this);
            }
#endif

            await socket.Connect();
        }

        public Task Send(byte[] data) => _ws.Send(data);

        public Task Close() => _ws.Close();

        public void CancelConnection() => _ws.CancelConnection();

#if !UNITY_WEBGL || UNITY_EDITOR
        public void DispatchMessageQueue()
        {
            lock (_dispatchLock)
            {
                if (_usesSharedDispatchLoop)
                {
                    _usesSharedDispatchLoop = false;
                    WebSocketDispatchLoop.Unregister(this);
                }

                _ws?.DispatchMessageQueue();
            }
        }

        internal void DispatchMessageQueueFromSharedLoop()
        {
            lock (_dispatchLock)
            {
                if (!_usesSharedDispatchLoop)
                {
                    return;
                }

                _ws?.DispatchMessageQueue();
            }
        }
#endif
    }
}

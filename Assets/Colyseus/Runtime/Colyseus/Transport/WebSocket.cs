using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Colyseus
{
    public class WebSocketTransport
    {
        public event WebSocketOpenEventHandler OnOpen;
        public event WebSocketMessageEventHandler OnMessage;
        public event WebSocketErrorEventHandler OnError;
        public event WebSocketCloseEventHandler OnClose;

        private NativeWebSocket.WebSocket _ws;
        private bool _processingMessages;

        public async Task Connect(string url, Dictionary<string, string> headers)
        {
            _ws = new NativeWebSocket.WebSocket(url, headers);

            _ws.OnOpen += () => OnOpen?.Invoke();
            _ws.OnMessage += (data) => OnMessage?.Invoke(data);
            _ws.OnError += (msg) => OnError?.Invoke(msg);
            _ws.OnClose += (code) =>
            {
                _processingMessages = false;
                ColyseusContext.UnregisterWebSocketForDispatch?.Invoke(_ws);
                OnClose?.Invoke((int)code);
            };

            if (ColyseusContext.RegisterWebSocketForDispatch != null)
            {
                // External dispatcher (MonoGame, Godot, etc.) handles DispatchMessageQueue
                ColyseusContext.RegisterWebSocketForDispatch(_ws);
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            else
            {
                // Fallback: self-dispatch via Task.Yield() loop
                ProcessMessageQueue();
            }
#endif

            await _ws.Connect();
        }

        public Task Send(byte[] data) => _ws.Send(data);

        public Task Close() => _ws.Close();

        public void CancelConnection() => _ws.CancelConnection();

#if !UNITY_WEBGL || UNITY_EDITOR
        private async void ProcessMessageQueue()
        {
            _processingMessages = true;
            try
            {
                while (_processingMessages)
                {
                    _ws?.DispatchMessageQueue();
                    await Task.Yield();
                }
            }
            catch (Exception e)
            {
                ColyseusContext.Logger?.LogError($"ProcessMessageQueue error: {e.Message}");
            }
        }
#endif
    }
}

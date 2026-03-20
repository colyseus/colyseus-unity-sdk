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
                OnClose?.Invoke((int)code);
            };

#if !UNITY_WEBGL || UNITY_EDITOR
            ProcessMessageQueue();
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
            while (_processingMessages)
            {
                _ws?.DispatchMessageQueue();
                await Task.Yield();
            }
        }
#endif
    }
}

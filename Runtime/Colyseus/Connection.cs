using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     WebSocket connection representation with some custom functionality
    /// </summary>
    public class Connection
    {
        public event WebSocketOpenEventHandler OnOpen;
        public event WebSocketMessageEventHandler OnMessage;
        public event WebSocketErrorEventHandler OnError;
        public event WebSocketCloseEventHandler OnClose;

        /// <summary>
        ///     Is the connection currently open
        /// </summary>
        public bool IsOpen;

        private WebSocketTransport _transport;
        private string _url;
        private Dictionary<string, string> _headers;

        public Connection(string url, Dictionary<string, string> headers)
        {
            _url = url;
            _headers = headers;
        }

        public async Task Connect()
        {
            _transport = new WebSocketTransport();

            _transport.OnOpen += () => { IsOpen = true; OnOpen?.Invoke(); };
            _transport.OnMessage += (data) => OnMessage?.Invoke(data);
            _transport.OnError += (msg) => OnError?.Invoke(msg);
            _transport.OnClose += (code) => { IsOpen = false; OnClose?.Invoke(code); };

            await _transport.Connect(_url, _headers);
        }

        public Task Send(byte[] data)
        {
            return _transport.Send(data);
        }

        public Task Close()
        {
            return _transport.Close();
        }

		public void Drop()
		{
			CancelConnection();
		}

        public void CancelConnection()
        {
            _transport?.CancelConnection();
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        /// <summary>
        /// Dispatch queued WebSocket callbacks manually from a custom game loop.
        /// This is only needed when no SynchronizationContext or external dispatcher is available.
        /// </summary>
        public void DispatchMessageQueue()
        {
            _transport?.DispatchMessageQueue();
        }
#endif

        /// <summary>
        ///     Reconnect to the same endpoint with a new reconnection token
        /// </summary>
        /// <param name="reconnectionToken">The token to use for reconnection</param>
        public async Task Reconnect(string reconnectionToken)
        {
            var uri = new Uri(_url);
            var queryParams = new List<string>();

            // Preserve existing query parameters
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var existingQuery = uri.Query.TrimStart('?');
                if (!string.IsNullOrEmpty(existingQuery))
                {
                    foreach (var param in existingQuery.Split('&'))
                    {
                        var key = param.Split('=')[0];
                        // Skip params we're going to override
                        if (key != "reconnectionToken" && key != "skipHandshake")
                        {
                            queryParams.Add(param);
                        }
                    }
                }
            }

            queryParams.Add("reconnectionToken=" + Uri.EscapeDataString(reconnectionToken));
            queryParams.Add("skipHandshake=1");

            var uriBuilder = new UriBuilder(uri) { Query = string.Join("&", queryParams) };
            _url = uriBuilder.ToString();

            ColyseusContext.Logger.Log($"Reconnecting to {_url}");
            await Connect();
        }
    }
}

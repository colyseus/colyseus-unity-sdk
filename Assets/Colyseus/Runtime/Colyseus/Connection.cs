using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     One thing the socket produced. Carrying every inbound kind in a single
    ///     value is what lets <see cref="Connection.Dispatch" /> be a single seam.
    /// </summary>
    public readonly struct ConnectionEvent
    {
        public enum Kind { Open, Message, Error, Close }

        public readonly Kind Type;
        /// <summary>Payload of a <see cref="Kind.Message" />.</summary>
        public readonly byte[] Data;
        /// <summary>Text of a <see cref="Kind.Error" />.</summary>
        public readonly string Reason;
        /// <summary>Code of a <see cref="Kind.Close" />.</summary>
        public readonly int Code;

        private ConnectionEvent(Kind type, byte[] data, string reason, int code)
        {
            Type = type; Data = data; Reason = reason; Code = code;
        }

        public static ConnectionEvent Opened() => new ConnectionEvent(Kind.Open, null, null, 0);
        public static ConnectionEvent Received(byte[] data) => new ConnectionEvent(Kind.Message, data, null, 0);
        public static ConnectionEvent Failed(string reason) => new ConnectionEvent(Kind.Error, null, reason, 0);
        public static ConnectionEvent Closed(int code) => new ConnectionEvent(Kind.Close, null, null, code);
    }

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
            Dispatch = Deliver;
            Transmit = data => _transport.Send(data);
        }

        /// <summary>
        ///     The single seam between the socket and the room: EVERY inbound
        ///     event goes through here.
        ///
        ///     Replace it to sit in front of every listener, including ones that
        ///     register later — capture the previous value and call it to pass an
        ///     event on. That is the whole contract, and it is why this is one
        ///     delegate rather than a per-event hook: subscribing to
        ///     <see cref="OnMessage" /> would only run you ALONGSIDE the room's
        ///     handler, so a latency simulator could not delay anything.
        ///
        ///     <code>
        ///     var inner = room.Connection.Dispatch;
        ///     room.Connection.Dispatch = e => queue.Add(e, () => inner(e));
        ///     </code>
        /// </summary>
        public Action<ConnectionEvent> Dispatch;

        /// <summary>Outbound twin of <see cref="Dispatch" />; wrap it the same way.</summary>
        public Func<byte[], Task> Transmit;

        public async Task Connect()
        {
            _transport = new WebSocketTransport();

            _transport.OnOpen += () => Dispatch(ConnectionEvent.Opened());
            _transport.OnMessage += data => Dispatch(ConnectionEvent.Received(data));
            _transport.OnError += reason => Dispatch(ConnectionEvent.Failed(reason));
            _transport.OnClose += code => Dispatch(ConnectionEvent.Closed(code));

            await _transport.Connect(_url, _headers);
        }

        /// <summary>Where an undelayed event ends up: the room's own listeners.</summary>
        private void Deliver(ConnectionEvent e)
        {
            switch (e.Type)
            {
                case ConnectionEvent.Kind.Open: IsOpen = true; OnOpen?.Invoke(); break;
                case ConnectionEvent.Kind.Message: OnMessage?.Invoke(e.Data); break;
                case ConnectionEvent.Kind.Error: OnError?.Invoke(e.Reason); break;
                case ConnectionEvent.Kind.Close: IsOpen = false; OnClose?.Invoke(e.Code); break;
            }
        }

        public Task Send(byte[] data)
        {
            return Transmit(data);
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

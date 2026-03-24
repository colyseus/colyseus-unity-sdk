using System;

namespace Colyseus
{
    public static class ColyseusContext
    {
        public static ILogger Logger { get; set; }
        public static IHttpClient HttpClient { get; set; }
        public static ITokenStorage TokenStorage { get; set; }

        /// <summary>
        /// When set, WebSocketTransport registers websockets here for external
        /// dispatching (e.g. MonoGame or a custom engine loop) instead of relying
        /// on SynchronizationContext dispatch or the shared fallback dispatcher.
        /// </summary>
        public static Action<NativeWebSocket.WebSocket> RegisterWebSocketForDispatch { get; set; }
        public static Action<NativeWebSocket.WebSocket> UnregisterWebSocketForDispatch { get; set; }

        static ColyseusContext()
        {
            SetDefaults();
        }

        public static void SetDefaults()
        {
            Logger = new ConsoleLogger();
            HttpClient = new DefaultHttpClient();
            TokenStorage = new InMemoryTokenStorage();
            RegisterWebSocketForDispatch = null;
            UnregisterWebSocketForDispatch = null;
        }
    }
}

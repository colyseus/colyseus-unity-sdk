using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Colyseus.MonoGame
{
    public class ColyseusGameComponent : GameComponent
    {
        private readonly List<NativeWebSocket.WebSocket> _sockets = new List<NativeWebSocket.WebSocket>();

        public ColyseusGameComponent(Game game) : base(game)
        {
            ColyseusContext.RegisterWebSocketForDispatch = (ws) =>
            {
                lock (_sockets) { _sockets.Add(ws); }
            };
            ColyseusContext.UnregisterWebSocketForDispatch = (ws) =>
            {
                lock (_sockets) { _sockets.Remove(ws); }
            };
        }

        public override void Update(GameTime gameTime)
        {
            lock (_sockets)
            {
                for (var i = 0; i < _sockets.Count; i++)
                {
                    _sockets[i].DispatchMessageQueue();
                }
            }
        }
    }
}

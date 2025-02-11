using UnityEngine;

namespace Colyseus.Schema
{
    public static class PacksExtensions
    {
        public static object AsPack(this Vector2 value)
        {
            return new
            {
                x = value.x,
                y = value.y,
            };
        }
    }
}

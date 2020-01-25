using System;
using System.Collections.Generic;

namespace Colyseus
{
    public static class ObjectExtensions
    {
        public static T ToObject<T>(object source) where T : class, new()
        {
            var someObject = new T();
            var someObjectType = someObject.GetType();

            foreach (var item in (IDictionary<string, object>)source) {
				var prop = someObjectType.GetProperty(item.Key);
				try
				{
					prop.SetValue(someObject, Convert.ChangeType(item.Value, prop.PropertyType), null);

				} catch (OverflowException) {
					// workaround for parsing Infinity on RoomAvailable.maxClients
					prop.SetValue(someObject, uint.MaxValue, null);
				}
            }

            return someObject;
        }
    }
}

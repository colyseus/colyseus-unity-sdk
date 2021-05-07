using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Colyseus
{
    /// <summary>
    ///     Extension methods for handling <see cref="IDictionary{TKey,TValue}" /> and <see cref="object" /> functionality
    /// </summary>
    public static class ColyseusObjectExtensions
    {
        /// <summary>
        ///     Convert an <see cref="IDictionary{TKey,TValue}" /> to an <see cref="object" /> of type <typeparamref name="T" />
        /// </summary>
        /// <param name="source">The dictionary to be converted</param>
        /// <typeparam name="T">The type of <see cref="object" /> we will convert this into</typeparam>
        /// <returns>An <see cref="object" /> of type <typeparamref name="T" /></returns>
        public static T ToObject<T>(this IDictionary<string, object> source)
            where T : class, new()
        {
            T someObject = new T();
            Type someObjectType = someObject.GetType();

            foreach (KeyValuePair<string, object> item in source)
            {
                someObjectType
                    .GetProperty(item.Key)
                    ?.SetValue(someObject, item.Value, null);
            }

            return someObject;
        }

        /// <summary>
        ///     Convert an <see cref="object" /> into a <see cref="IDictionary{TKey,TValue}"></see>
        /// </summary>
        /// <param name="source">The <see cref="object" /> to be converted</param>
        /// <param name="bindingAttr">The <see cref="BindingFlags" /> to use on this <see cref="object" /></param>
        /// <returns>An <see cref="IDictionary{TKey,TValue}" /> version of the <paramref name="source" /></returns>
        public static IDictionary<string, object> AsDictionary(this object source,
            BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );
        }
    }
}
#if !NET_LEGACY

namespace Colyseus
{
    /// <summary>
    ///     Utility class for HTTP based functions
    /// </summary>
    public class ColyseusHttpUtility
    {
        /// <summary>
        ///     Currently used as a constructor for a new <see cref="ColyseusHttpQSCollection" /> - Needs revisiting
        /// </summary>
        /// <param name="str">Unused and currently only being passed in as String.Empty</param>
        /// <returns></returns>
        //TODO: Why does this function not appear to parse anything? Currently just used as a constructor and only ever provided with a String.Empty as the str parameter, which is then never used
        public static ColyseusHttpQSCollection ParseQueryString(string str)
        {
            return new ColyseusHttpQSCollection();
        }
    }
}

#endif
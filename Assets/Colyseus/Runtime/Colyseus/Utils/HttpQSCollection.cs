#if !NET_LEGACY
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace Colyseus
{
    /// <summary>
    ///     Child class used to convert a <see cref="NameValueCollection" /> to a properly formatted Query String for HTTP
    ///     requests
    /// </summary>
    public class ColyseusHttpQSCollection : NameValueCollection
    {
        /// <summary>
        ///     Build a Query string out of all the keys in this <see cref="NameValueCollection" />
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            int count = Count;
            if (count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            string[] keys = AllKeys;
            for (int i = 0; i < count; i++)
            {
                sb.AppendFormat("{0}={1}&", keys[i], WebUtility.UrlEncode(this[keys[i]]));
            }

            if (sb.Length > 0)
            {
                sb.Length--;
            }

            return sb.ToString();
        }
    }

}
#endif
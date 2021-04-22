using System.Text.RegularExpressions;

namespace Colyseus
{
    /// <summary>
    ///     Wrapper struct for listening to patch changes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Listener<T>
    {
        /// <summary>
        ///     Callback to invoke when a data change occurs
        /// </summary>
        public T callback;

        public Regex[] rules;
        public string[] rawRules;
    }
}
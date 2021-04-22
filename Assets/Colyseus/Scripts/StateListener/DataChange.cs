using System.Collections.Generic;

namespace Colyseus
{
    /// <summary>
    ///     Wrapper struct to represent the change from a state to another
    /// </summary>
    public struct DataChange
    {
        /// <summary>
        ///     The change path
        /// </summary>
        public Dictionary<string, string> path;

        /// <summary>
        ///     The change operation
        ///     <para>Expected Values:</para>
        ///     <para>
        ///         <em>"add" | "remove" | "replace"</em>
        ///     </para>
        /// </summary>
        public string operation; // : "add" | "remove" | "replace";

        /// <summary>
        ///     The value included in the change
        /// </summary>
        public object value;
    }
}

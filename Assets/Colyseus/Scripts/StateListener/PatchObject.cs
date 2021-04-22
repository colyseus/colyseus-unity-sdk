namespace Colyseus
{
    public struct PatchObject
    {
        /// <summary>
        ///     The location of the patch
        /// </summary>
        public string[] path;

        /// <summary>
        ///     The patch's operation
        ///     <para>Expected Values:</para>
        ///     <para>
        ///         <em>"add" | "remove" | "replace"</em>
        ///     </para>
        /// </summary>
        public string operation; // : "add" | "remove" | "replace";

        /// <summary>
        ///     The patch <see cref="object" />
        /// </summary>
        public object value;
    }
}
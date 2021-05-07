using System.Collections.Generic;

namespace Colyseus
{
    /// <summary>
    ///     Class for various needed Array utilities
    /// </summary>
    public class ColyseusArrayUtils
    {
        /// <summary>
        ///     Get a partial array from a larger array
        /// </summary>
        /// <param name="bytes">The original array</param>
        /// <param name="index">Starting index of our new array</param>
        /// <param name="length">Length of our new array</param>
        /// <returns>An array created from a chunk of the original array</returns>
        public static byte[] SubArray(byte[] bytes, int index, int length)
        {
            return new List<byte>(bytes).GetRange(index, length).ToArray();
        }
    }
}
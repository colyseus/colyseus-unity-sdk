using System.Collections.Generic;

namespace Colyseus
{
	public class ArrayUtils
	{
		public static byte[] SubArray(byte[] bytes, int index, int length)
		{
			return new List<byte>(bytes).GetRange(index, length).ToArray();
		}
	}
}

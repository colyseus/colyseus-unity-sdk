using MsgPack;
using System;
using System.IO;
using System.Runtime.InteropServices;
#if WINDOWS_UWP
using Windows.Storage.Streams;
#else
using WebSocketSharp;
#endif

namespace Helper {
    public static class Helper
    {
		/*
        public static byte[] ToByteArray (this Object str, ByteOrder byteOrder)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
		*/

		/// <summary>
		/// from https://github.com/sta/websocket-sharp/blob/master/websocket-sharp/Ext.cs#L1828
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="order"></param>
		/// <returns></returns>
		public static byte[] ToByteArray<T>(this T value, ByteOrder order)
			where T : struct
		{
			var type = typeof(T);
			var bytes = type == typeof(Boolean) 
				? BitConverter.GetBytes((Boolean)(object) value) 
				: type == typeof (Byte) 
					? new byte[] { (Byte) (object) value } 
					: type == typeof (Char) 
						? BitConverter.GetBytes((Char)(object) value)
						: type == typeof (Double) 
							? BitConverter.GetBytes((Double)(object) value)
							: type == typeof (Int16) 
								? BitConverter.GetBytes((Int16)(object) value)
								: type == typeof (Int32) 
									? BitConverter.GetBytes((Int32)(object) value)
									: type == typeof (Int64) 
										? BitConverter.GetBytes((Int64)(object) value)
										: type == typeof (Single) 
											? BitConverter.GetBytes((Single)(object) value)
											: type == typeof (UInt16) 
												? BitConverter.GetBytes((UInt16)(object) value)
												: type == typeof (UInt32) 
													? BitConverter.GetBytes((UInt32)(object) value)
													: type == typeof (UInt64) 
														? BitConverter.GetBytes((UInt64)(object) value)
														: new byte[0];

			/*
			if (bytes.Length > 1 && !order.IsHostOrder())
				Array.Reverse(bytes);
				*/
			return bytes;
		} 
	}
}


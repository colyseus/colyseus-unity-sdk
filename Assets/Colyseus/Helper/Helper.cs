using MsgPack;
using System;
using System.Runtime.InteropServices;
#if WINDOWS_UWP
using Windows.Storage.Streams;
#else
using WebSocketSharp;
#endif

namespace Helper {
    public static class Helper
    {
        // 
        public static byte[] ToByteArray (this MessagePackObject str, ByteOrder byteOrder)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}


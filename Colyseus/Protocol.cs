using System;

namespace Colyseus
{
	public class Protocol
	{
		public static int USER_ID = 1;

		// Room-related (10~20)
		public static int JOIN_ROOM = 10;
		public static int JOIN_ERROR = 12;
		public static int LEAVE_ROOM = 12;
		public static int ROOM_DATA = 13;
		public static int ROOM_STATE = 14;
		public static int ROOM_STATE_PATCH = 15;

		// Generic messages (50~60)
		public static int BAD_REQUEST = 50;

		public Protocol (){}
	}
}


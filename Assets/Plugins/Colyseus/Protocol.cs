using System;

namespace Colyseus
{
	/// <summary>
	/// Colyseus server protocol codes mapping.
	/// </summary>
	public class Protocol
	{
		/// <summary>When client receives its unique id.</summary>
		public static int USER_ID = 1;

		//
		// Room-related (9~19)
		//

		/// <summary>When JOIN is requested.</summary>
		public static int JOIN_REQUEST = 9;

		/// <summary>When JOIN request is accepted.</summary>
		public static int JOIN_ROOM = 10;

		/// <summary>When JOIN request is not accepted.</summary>
		public static int JOIN_ERROR = 11;

		/// <summary>When server explicitly removes <see cref="Client"/> from the <see cref="Room"/></summary>
		public static int LEAVE_ROOM = 12;

		/// <summary>When server sends data to a particular <see cref="Room"/></summary>
		public static int ROOM_DATA = 13;

		/// <summary>When server sends <see cref="Room"/> state to its clients.</summary>
		public static int ROOM_STATE = 14;

		/// <summary>When server sends <see cref="Room"/> state to its clients.</summary>
		public static int ROOM_STATE_PATCH = 15;

		// 
		// Matchmaking messages (20~30)
		// 
		public static int ROOM_LIST = 20;

		// 
		// Generic messages (50~60)
		// 

		/// <summary>When server doesn't understand a request, it returns <see cref="BAD_REQUEST"/> to the <see cref="Client"/></summary>
		public static int BAD_REQUEST = 50;

		// public Protocol (){}
	}
}


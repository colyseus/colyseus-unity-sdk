using System;

namespace Colyseus
{
	public class MatchMakeException : Exception
	{
		public int Code;
		public MatchMakeException(int code, string message) : base(message)
		{
			Code = code;
		}
	}
}

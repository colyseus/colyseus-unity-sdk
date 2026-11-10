// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     Colyseus error codes mapping.
    /// </summary>
    public class ErrorCode
    {
	    public static int MATCHMAKE_NO_HANDLER = 520;
	    public static int MATCHMAKE_INVALID_CRITERIA = 521;
	    public static int MATCHMAKE_INVALID_ROOM_ID = 522;
	    public static int MATCHMAKE_UNHANDLED = 523;
	    public static int MATCHMAKE_EXPIRED = 524;

	    public static int AUTH_FAILED = 525;
	    public static int APPLICATION_ERROR = 526;

	    /// <summary>
	    ///     When local schema is different from schema on the server.
	    /// </summary>
	    public static int SCHEMA_MISMATCH = 4300;
    }
}

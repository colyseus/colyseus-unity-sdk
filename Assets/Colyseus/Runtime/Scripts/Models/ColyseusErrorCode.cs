// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     Colyseus error codes mapping.
    /// </summary>
    public class ColyseusErrorCode
    {
	    public static int MATCHMAKE_NO_HANDLER = 4210;
	    public static int MATCHMAKE_INVALID_CRITERIA = 4211;
	    public static int MATCHMAKE_INVALID_ROOM_ID = 4212;
	    public static int MATCHMAKE_UNHANDLED = 4213;
	    public static int MATCHMAKE_EXPIRED = 4214;

	    public static int AUTH_FAILED = 4215;
	    public static int APPLICATION_ERROR = 4216;

	    /// <summary>
	    ///     When local schema is different from schema on the server.
	    /// </summary>
	    public static int SCHEMA_MISMATCH = 4217;
    }
}
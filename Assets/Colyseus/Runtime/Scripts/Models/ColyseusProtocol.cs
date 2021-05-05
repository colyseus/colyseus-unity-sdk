// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     Colyseus server protocol codes mapping.
    /// </summary>
    public class ColyseusProtocol
    {
        /// <summary>
        ///     When client receives its unique id.
        /// </summary>
        public static byte USER_ID = 1;

        //
        // Room-related (9~19)
        //

        /// <summary>
        ///     When JOIN is requested.
        /// </summary>
        public static byte JOIN_REQUEST = 9;

        /// <summary>
        ///     When JOIN request is accepted.
        /// </summary>
        public static byte JOIN_ROOM = 10;

        /// <summary>
        ///     When an error has happened in the server-side.
        /// </summary>
        public static byte ERROR = 11;

        /// <summary>
        ///     When server explicitly removes <see cref="ColyseusClient" /> from the <see cref="ColyseusRoom{T}" />
        /// </summary>
        public static byte LEAVE_ROOM = 12;

        /// <summary>
        ///     When server sends data to a particular <see cref="ColyseusRoom{T}" />
        /// </summary>
        public static byte ROOM_DATA = 13;

        /// <summary>
        ///     When server sends <see cref="ColyseusRoom{T}" /> state to its clients.
        /// </summary>
        public static byte ROOM_STATE = 14;

        /// <summary>
        ///     When server sends <see cref="ColyseusRoom{T}" /> state to its clients.
        /// </summary>
        public static byte ROOM_STATE_PATCH = 15;

        /// <summary>
        ///     When server sends a Schema-encoded message.
        /// </summary>
        public static byte ROOM_DATA_SCHEMA = 16;

        //
        // Matchmaking messages (20~30)
        //
        public static byte ROOM_LIST = 20;

        //
        // Generic messages (50~60)
        //

        /// <summary>
        ///     When server doesn't understand a request, it returns <see cref="BAD_REQUEST" /> to the <see cref="ColyseusClient" />
        /// </summary>
        public static byte BAD_REQUEST = 50;

        // public Protocol (){}
    }
}
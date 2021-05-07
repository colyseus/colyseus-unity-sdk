using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colyseus
{
    /// <summary>
    ///     Wrapper class for a match making response
    /// </summary>
    /// <remarks>Returns room and sessionId if successful; code and error if not</remarks>
    [Serializable]
    public class ColyseusMatchMakeResponse
    {
        /// <summary>
        ///     Error code if response is error
        /// </summary>
        public int code;

        /// <summary>
        ///     Error information if response is an error
        /// </summary>
        public string error;

        /// <summary>
        ///     Room information on successful match making
        /// </summary>
        public ColyseusRoomAvailable room;

        /// <summary>
        ///     Session ID used for connection to the room
        /// </summary>
        public string sessionId;
    }
}

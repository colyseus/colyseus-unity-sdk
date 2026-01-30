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
    public class SeatReservation
    {
        public string name;
        public string sessionId;
        public string roomId;
        public string publicAddress;
        public string processId;
        public string reconnectionToken;
        public bool devMode;
        public string protocol;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colyseus
{
    /// <summary>
    ///     Wrapper class for important, shorthand Room information
    /// </summary>
    [Serializable]
    public class ColyseusRoomAvailable
    {
        /// <summary>
        ///     Current client count
        /// </summary>
        public uint clients;

        /// <summary>
        ///     Maximum clients in this room
        /// </summary>
        public uint maxClients;

        /// <summary>
        ///     Room name
        /// </summary>
        public string name;

        /// <summary>
        ///     Process ID used for connection
        /// </summary>
        public string processId;

        /// <summary>
        ///     Room ID
        /// </summary>
        public string roomId;

        // public object metadata;
    }

    /// <summary>
    ///     Get a collection of rooms
    /// </summary>
    /// <typeparam name="T">Type of room inherited from <see cref="ColyseusRoomAvailable" /></typeparam>
    [Serializable]
    public class CSARoomAvailableCollection<T>
    {
        /// <summary>
        ///     Rooms in this collection
        /// </summary>
        public T[] rooms;
    }
}

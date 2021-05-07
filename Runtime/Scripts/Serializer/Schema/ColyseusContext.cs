using System.Collections.Generic;

namespace Colyseus.Schema
{
    /// <summary>
    ///     Internal container class for tracking and maintaining the context of the current application
    /// </summary>
    internal class ColyseusContext
    {
        /// <summary>
        ///     Singleton instance
        /// </summary>
        protected static ColyseusContext instance = new ColyseusContext();

        /// <summary>
        ///     Schema types and IDs we're aware of
        /// </summary>
        protected Dictionary<float, System.Type> typeIds = new Dictionary<float, System.Type>();

        protected List<System.Type> types = new List<System.Type>(); //TODO: This does not appear to be used ever

        /// <summary>
        ///     Getter function for the singleton <see cref="instance" />
        /// </summary>
        /// <returns>The singleton <see cref="instance" /></returns>
        public static ColyseusContext GetInstance()
        {
            return instance;
        }

        /// <summary>
        ///     Add a new Schema type by id
        /// </summary>
        /// <param name="type">The incoming schema type</param>
        /// <param name="typeid">The schema type ID we received via server Handshake</param>
        public void SetTypeId(System.Type type, float typeid)
        {
            typeIds[typeid] = type;
        }

        /// <summary>
        ///     Get the <see cref="System.Type" /> with a given <paramref name="typeid" />
        /// </summary>
        /// <param name="typeid">The schema type ID</param>
        /// <returns>The schema type we previous have set with <see cref="SetTypeId" /></returns>
        public System.Type Get(float typeid)
        {
            return typeIds[typeid]; //TODO: Confirm if this can ever fail and if so we should handle it gracefully
        }
    }
}
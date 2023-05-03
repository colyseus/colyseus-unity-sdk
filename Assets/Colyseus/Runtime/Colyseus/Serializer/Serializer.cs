namespace Colyseus
{
    /// <summary>
    ///     Serializer Interface
    /// </summary>
    /// <typeparam name="T">The type of state this serializer will be for</typeparam>
    public interface IColyseusSerializer<T>
    {
        /// <summary>
        ///     Set the serializer's state
        /// </summary>
        /// <param name="data">The incoming state data</param>
        /// <param name="offset">Offset for reading the incoming data</param>
        void SetState(byte[] data, int offset);

        /// <summary>
        ///     Get the current state
        /// </summary>
        /// <returns>An object of type <typeparamref name="T" /> representing the current state</returns>
        T GetState();

        /// <summary>
        ///     Apply a patch to this state
        /// </summary>
        /// <param name="data">The incoming state data</param>
        /// <param name="offset">Offset for reading the incoming data</param>
        void Patch(byte[] data, int offset);

        /// <summary>
        ///     Clean-up functionality
        /// </summary>
        void Teardown();

        /// <summary>
        ///     Confirms connection and serialization is working properly
        /// </summary>
        /// <param name="bytes">The handshake data to serialize</param>
        /// <param name="offset">Offset for reading the incoming data</param>
        void Handshake(byte[] bytes, int offset);
    }
}
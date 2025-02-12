namespace Colyseus
{
    // TODO: remove dummy state dependency from NoneSerializer.
    public class NoState : Schema.Schema { }

    /// <summary>
    ///     An empty implementation of <see cref="IColyseusSerializer{T}" />
    /// </summary>
    public class NoneSerializer<T> : IColyseusSerializer<object> where T: NoState
    {
        /// <inheritdoc />
        public void SetState(byte[] rawEncodedState, int offset)
        {
        }

        /// <inheritdoc />
        public object GetState()
        {
            return this;
        }

        /// <inheritdoc />
        public void Patch(byte[] bytes, int offset)
        {
        }

        /// <inheritdoc />
        public void Teardown()
        {
        }

        /// <inheritdoc />
        public void Handshake(byte[] bytes, int offset)
        {
        }
    }
}
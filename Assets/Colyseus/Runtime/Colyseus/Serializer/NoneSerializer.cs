namespace Colyseus
{
    // TODO: remove dummy state dependency from NoneSerializer.
    public class NoState : Schema.Schema { }

    /// <summary>
    ///     An empty implementation of <see cref="ISerializer{T}" />
    /// </summary>
    public class NoneSerializer : ISerializer<NoState>
    {
        NoState state = new NoState();

        /// <inheritdoc />
        public void SetState(byte[] rawEncodedState, int offset)
        {
        }

        /// <inheritdoc />
        public NoState GetState()
        {
            return state;
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
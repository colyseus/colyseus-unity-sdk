namespace Colyseus
{
	public class NoneSerializer : ISerializer<object>
	{

		public void SetState(byte[] rawEncodedState, int offset) {}
		public object GetState() { return this; }
		public void Patch(byte[] bytes, int offset) { }
		public void Teardown() {}
		public void Handshake(byte[] bytes, int offset) { }

	}
}

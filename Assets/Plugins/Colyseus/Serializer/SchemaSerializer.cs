using System;

namespace Colyseus
{
	public class SchemaSerializer<T> : ISerializer<T> // where T : Colyseus.Schema.Schema
	{
		protected T state;

		public SchemaSerializer()
		{
			state = Activator.CreateInstance<T>();
		}

		public void SetState(byte[] data)
		{
			(state as Schema.Schema).Decode(data);
		}

		public T GetState()
		{
			return state;
		}

		public void Patch(byte[] data)
		{
			(state as Schema.Schema).Decode(data);
		}

	    public void Teardown ()
		{
		}

    	public void Handshake (byte[] bytes, int offset)
		{
			// TODO: decode reflected schema 
			// TODO: validate if local schema matches version from the server.
		}
	}
}

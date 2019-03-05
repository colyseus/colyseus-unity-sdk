using System;
using System.IO;
using GameDevWare.Serialization;

namespace Colyseus
{
	public class FossilDeltaSerializer<T> : Serializer<T>
	{
		protected StateContainer state = new StateContainer(new IndexedDictionary<string, object>());
		protected byte[] previousState = null;

		void SetState(byte[] encodedState)
		{
			Set(MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(encodedState)));
			previousState = encodedState;
		}

		T GetState()
		{
			return state.state;
		}

		void Patch(byte[] data)
		{
			previousState = Fossil.Delta.Apply (previousState, delta);
			var newState = MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(previousState));
			Set(newState);
		}

	    void Teardown ()
		{
			state.RemoveAllListeners();
		}

    	void Handshake (byte[] bytes)
		{
		}
	}
}

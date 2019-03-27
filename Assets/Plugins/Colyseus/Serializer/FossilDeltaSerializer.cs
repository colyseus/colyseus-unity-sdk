using UnityEngine;
using System.IO;
using GameDevWare.Serialization;

namespace Colyseus
{
	/* public class FossilDeltaSerializer<T> : Serializer<T> */
	public class FossilDeltaSerializer : ISerializer<IndexedDictionary<string, object>>
	{
		public StateContainer State = new StateContainer(new IndexedDictionary<string, object>());
		protected byte[] previousState = null;

		public void SetState(byte[] encodedState)
        {
			State.Set(MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(encodedState)));
			previousState = encodedState;
		}

		public IndexedDictionary<string, object> GetState()
		{
			return State.state;
		}

		public void Patch(byte[] bytes)
		{
			previousState = Fossil.Delta.Apply (previousState, bytes);
			var newState = MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(previousState));
			State.Set(newState);
		}

	    public void Teardown ()
		{
			State.RemoveAllListeners();
		}

    	public void Handshake (byte[] bytes, int offset)
		{
			Debug.Log("Handshake FossilDeltaSerializer!");
		}
	}
}

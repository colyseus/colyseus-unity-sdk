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

		public void SetState(byte[] encodedState, int offset)
        {
			State.Set(MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(
				ArrayUtils.SubArray(encodedState, offset, encodedState.Length - 1)
			)));
			previousState = encodedState;
		}

		public IndexedDictionary<string, object> GetState()
		{
			return State.state;
		}

		public void Patch(byte[] bytes, int offset)
		{
			previousState = Fossil.Delta.Apply (previousState, ArrayUtils.SubArray(bytes, offset, bytes.Length - 1));
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

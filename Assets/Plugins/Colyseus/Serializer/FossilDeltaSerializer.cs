using UnityEngine;
using System.IO;
using System.Text;
using GameDevWare.Serialization;

namespace Colyseus
{
	/* public class FossilDeltaSerializer<T> : Serializer<T> */
	public class FossilDeltaSerializer : ISerializer<IndexedDictionary<string, object>>
	{
		public StateContainer State = new StateContainer(new IndexedDictionary<string, object>());
		protected byte[] previousState = null;

		public void SetState(byte[] rawEncodedState, int offset)
        {
			//Debug.Log("FULL STATE");
			//PrintByteArray(rawEncodedState);
			previousState = ArrayUtils.SubArray(rawEncodedState, offset, rawEncodedState.Length - 1);
			State.Set(MsgPack.Deserialize<IndexedDictionary<string, object>> (new MemoryStream(previousState)));
		}

		public IndexedDictionary<string, object> GetState()
		{
			return State.state;
		}

		public void Patch(byte[] bytes, int offset)
		{
			//Debug.Log("PATCH STATE");
			//PrintByteArray(bytes);
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

		//public void PrintByteArray(byte[] bytes)
		//{
		//	var sb = new StringBuilder("[");
		//	foreach (var b in bytes)
		//	{
		//		sb.Append(b + ", ");
		//	}
		//	sb.Append("]");
		//	Debug.Log(sb.ToString());
		//}

	}
}

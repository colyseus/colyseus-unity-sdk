using System;
using GameDevWare.Serialization;

namespace Colyseus
{
	public interface ISerializer<T>
	{
		void SetState(byte[] data);
		T GetState();
		//IndexedDictionary<string, object> GetState();
		void Patch(byte[] data);

	    void Teardown ();
    	void Handshake (byte[] bytes, int offset);
	}
}

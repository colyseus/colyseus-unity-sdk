using System;

namespace Colyseus
{
	public interface Serializer<T>
	{
		void SetState(byte[] data);
		T GetState();
		void Patch(byte[] data);

	    void Teardown ();
    	void Handshake (byte[] bytes);
	}
}

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

	public class NotSerializableException : Exception
	{
	  public MyException() { }
	  public MyException( string message ) : base( message ) { }
	  public MyException( string message, Exception inner ) : base( message, inner ) { }
	  protected MyException( 
		System.Runtime.Serialization.SerializationInfo info, 
		System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
	}
}

using System;

namespace Colyseus
{
	public class Room
	{
		protected Client client;
		public String name;

		public int id = 0;
		public object state;

		public Room (Client client, String name)
		{
			this.client = client;
			this.name = name;
			this.state = null;
		}

		public void Leave () 
		{
		}

		public void Send<T> (T data)
		{
			
		}
	}
}


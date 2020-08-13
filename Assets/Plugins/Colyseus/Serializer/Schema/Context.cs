using System;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	class Context
	{
		protected static Context instance = new Context();
		protected List<System.Type> types = new List<System.Type>();
		protected Dictionary<int, System.Type> typeIds = new Dictionary<int, System.Type>();

		public static Context GetInstance()
		{
			return instance;
		}

		public void SetTypeId(System.Type type, int typeid)
		{
			typeIds[typeid] = type;
		}

		public System.Type Get(int typeid)
		{
			return typeIds[typeid];
		}
	}
}
using System;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	class Context
	{
		protected static Context instance = new Context();
		protected List<System.Type> types = new List<System.Type>();
		protected Dictionary<float, System.Type> typeIds = new Dictionary<float, System.Type>();

		public static Context GetInstance()
		{
			return instance;
		}

		public void SetTypeId(System.Type type, float typeid)
		{
			typeIds[typeid] = type;
		}

		public System.Type Get(float typeid)
		{
			return typeIds[typeid];
		}
	}
}
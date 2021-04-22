using System;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	class ColyseusCustomType
	{
		protected static ColyseusCustomType instance = new ColyseusCustomType();
		protected Dictionary<string, System.Type> types = new Dictionary<string, System.Type>();
		//{
		//		["array"] = ArraySchema
		//};	

		public static ColyseusCustomType GetInstance()
		{
			return instance;
		}

		public void Add(string id, System.Type type)
		{
			if (!types.ContainsKey(id))
			{
				types.Add(id, type);
			}
		}

		public System.Type Get(string id)
		{
			System.Type type;
			types.TryGetValue(id, out type);
			return type;
		}
	}
}

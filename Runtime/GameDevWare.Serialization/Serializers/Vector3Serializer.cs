/* 
	Copyright (c) 2019 Denis Zykov, GameDevWare.com

	This a part of "Json & MessagePack Serialization" Unity Asset - https://www.assetstore.unity3d.com/#!/content/59918

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/
#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_5_3_OR_NEWER
using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class Vector3Serializer : TypeSerializer
	{
		public override Type SerializedType { get { return typeof(Vector3); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.Null)
				return null;

			var value = new Vector3();
			reader.ReadObjectBegin();
			while (reader.Token != JsonToken.EndOfObject)
			{
				var memberName = reader.ReadMember();
				switch (memberName)
				{
					case "x": value.x = reader.ReadSingle(); break;
					case "y": value.y = reader.ReadSingle(); break;
					case "z": value.z = reader.ReadSingle(); break;
					default: reader.ReadValue(typeof(object)); break;
				}
			}
			reader.ReadObjectEnd(nextToken: false);
			return value;
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var vector3 = (Vector3)value;
			writer.WriteObjectBegin(3);
			writer.WriteMember("x");
			writer.Write(vector3.x);
			writer.WriteMember("y");
			writer.Write(vector3.y);
			writer.WriteMember("z");
			writer.Write(vector3.z);
			writer.WriteObjectEnd();
		}
	}
}
#endif

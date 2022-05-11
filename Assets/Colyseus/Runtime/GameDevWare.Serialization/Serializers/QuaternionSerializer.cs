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
	public sealed class QuaternionSerializer : TypeSerializer
	{
		public override Type SerializedType { get { return typeof(Quaternion); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.Null)
				return null;

			var value = new Quaternion();
			reader.ReadObjectBegin();
			while (reader.Token != JsonToken.EndOfObject)
			{
				var memberName = reader.ReadMember();
				if (memberName == "x")
					value.x = reader.ReadSingle();
				else if (memberName == "y")
					value.y = reader.ReadSingle();
				else if (memberName == "z")
					value.z = reader.ReadSingle();
				else if (memberName == "w")
					value.w = reader.ReadSingle();
				else
					reader.ReadValue(typeof(object));
			}
			reader.ReadObjectEnd(nextToken: false);
			return value;
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var quaternion = (Quaternion)value;
			writer.WriteObjectBegin(4);
			writer.WriteMember("x");
			writer.Write(quaternion.x);
			writer.WriteMember("y");
			writer.Write(quaternion.y);
			writer.WriteMember("z");
			writer.Write(quaternion.z);
			writer.WriteMember("w");
			writer.Write(quaternion.w);
			writer.WriteObjectEnd();
		}
	}
}
#endif

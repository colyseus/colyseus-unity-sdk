/* 
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

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
#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class Matrix4x4Serializer : TypeSerializer
	{
		public override Type SerializedType { get { return typeof(Matrix4x4); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token == JsonToken.Null)
				return null;

			var value = new Matrix4x4();
			reader.ReadObjectBegin();
			while (reader.Token != JsonToken.EndOfObject)
			{
				var memberName = reader.ReadMember();
				switch (memberName)
				{
					case "m00": value.m00 = reader.ReadSingle(); break;
					case "m10": value.m10 = reader.ReadSingle(); break;
					case "m20": value.m20 = reader.ReadSingle(); break;
					case "m30": value.m30 = reader.ReadSingle(); break;
					case "m01": value.m01 = reader.ReadSingle(); break;
					case "m11": value.m11 = reader.ReadSingle(); break;
					case "m21": value.m21 = reader.ReadSingle(); break;
					case "m31": value.m31 = reader.ReadSingle(); break;
					case "m02": value.m02 = reader.ReadSingle(); break;
					case "m12": value.m12 = reader.ReadSingle(); break;
					case "m22": value.m22 = reader.ReadSingle(); break;
					case "m32": value.m32 = reader.ReadSingle(); break;
					case "m03": value.m03 = reader.ReadSingle(); break;
					case "m13": value.m13 = reader.ReadSingle(); break;
					case "m23": value.m23 = reader.ReadSingle(); break;
					case "m33": value.m33 = reader.ReadSingle(); break;
					default:
						reader.ReadValue(typeof(object));
						break;
				}
			}
			reader.ReadObjectEnd(nextToken: false);
			return value;
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var matrix = (Matrix4x4)value;
			writer.WriteObjectBegin(16);
			writer.WriteMember("m00");
			writer.Write(matrix.m00);
			writer.WriteMember("m10");
			writer.Write(matrix.m10);
			writer.WriteMember("m20");
			writer.Write(matrix.m20);
			writer.WriteMember("m30");
			writer.Write(matrix.m30);
			writer.WriteMember("m01");
			writer.Write(matrix.m01);
			writer.WriteMember("m11");
			writer.Write(matrix.m11);
			writer.WriteMember("m21");
			writer.Write(matrix.m21);
			writer.WriteMember("m31");
			writer.Write(matrix.m31);
			writer.WriteMember("m02");
			writer.Write(matrix.m02);
			writer.WriteMember("m12");
			writer.Write(matrix.m12);
			writer.WriteMember("m22");
			writer.Write(matrix.m22);
			writer.WriteMember("m32");
			writer.Write(matrix.m32);
			writer.WriteMember("m03");
			writer.Write(matrix.m03);
			writer.WriteMember("m13");
			writer.Write(matrix.m13);
			writer.WriteMember("m23");
			writer.Write(matrix.m23);
			writer.WriteMember("m33");
			writer.Write(matrix.m33);
			writer.WriteObjectEnd();
		}
	}
}
#endif

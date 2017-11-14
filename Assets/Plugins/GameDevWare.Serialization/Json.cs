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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDevWare.Serialization.MessagePack;
using GameDevWare.Serialization.Serializers;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public static class Json
	{
		
		private static IFormatProvider _DefaultFormat = CultureInfo.InvariantCulture;
		private static Encoding _DefaultEncoding = new UTF8Encoding(false, true);
		private static string[] _DefaultDateTimeFormats;

		public static string[] DefaultDateTimeFormats
		{
			get { return _DefaultDateTimeFormats; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");
				if (value.Length == 0) throw new ArgumentException();

				_DefaultDateTimeFormats = value;
			}
		}
		public static IFormatProvider DefaultFormat
		{
			get { return _DefaultFormat; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");

				_DefaultFormat = value;
			}
		}
		public static Encoding DefaultEncoding
		{
			get { return _DefaultEncoding; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");

				_DefaultEncoding = value;
			}
		}
		public static List<TypeSerializer> DefaultSerializers { get; private set; }

		static Json()
		{
			// ReSharper disable StringLiteralTypo
			_DefaultDateTimeFormats = new[]
			{
				"yyyy-MM-ddTHH:mm:ss.fffzzz", // ISO 8601, with timezone
				"yyyy-MM-ddTHH:mm:ss.ffzzz", // ISO 8601, with timezone
				"yyyy-MM-ddTHH:mm:ss.fzzz", // ISO 8601, with timezone
				"yyyy-MM-ddTHH:mm:ssZ", // also ISO 8601, without timezone and without microseconds
				"yyyy-MM-ddTHH:mm:ss.fZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.ffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.fffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.ffffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.fffffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.ffffffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.fffffffZ" // also ISO 8601, without timezone
			};
			// ReSharper restore StringLiteralTypo

			DefaultSerializers = new List<TypeSerializer>
			{
				new BinarySerializer(),
				new DateTimeOffsetSerializer(),
				new DateTimeSerializer(),
				new GuidSerializer(),
				new StreamSerializer(),
				new UriSerializer(),
				new VersionSerializer(),
				new TimeSpanSerializer(),
				new DictionaryEntrySerializer(),

#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
				new BoundsSerializer(),
				new Matrix4x4Serializer(),
				new QuaternionSerializer(),
				new RectSerializer(),
				new Vector2Serializer(),
				new Vector3Serializer(),
				new Vector4Serializer(),
#endif
				new PrimitiveSerializer(typeof (bool)),
				new PrimitiveSerializer(typeof (byte)),
				new PrimitiveSerializer(typeof (decimal)),
				new PrimitiveSerializer(typeof (double)),
				new PrimitiveSerializer(typeof (short)),
				new PrimitiveSerializer(typeof (int)),
				new PrimitiveSerializer(typeof (long)),
				new PrimitiveSerializer(typeof (sbyte)),
				new PrimitiveSerializer(typeof (float)),
				new PrimitiveSerializer(typeof (ushort)),
				new PrimitiveSerializer(typeof (uint)),
				new PrimitiveSerializer(typeof (ulong)),
				new PrimitiveSerializer(typeof (string)),
			};
		}

		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(SerializationOptions.None));
		}
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, Encoding encoding)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(SerializationOptions.None, encoding));
		}
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, SerializationOptions options)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(options));
		}
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, SerializationOptions options, Encoding encoding)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(options, encoding));
		}
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, SerializationContext context)
		{
			if (jsonOutput == null) throw new ArgumentNullException("jsonOutput");
			if (context == null) throw new ArgumentNullException("context");
			if (!jsonOutput.CanWrite) throw JsonSerializationException.StreamIsNotWriteable();


			if (objectToSerialize == null)
			{
				var bytes = context.Encoding.GetBytes("null");
				jsonOutput.Write(bytes, 0, bytes.Length);
				return;
			}

			var writer = new JsonStreamWriter(jsonOutput, context);
			writer.WriteValue(objectToSerialize, typeof(T));
			writer.Flush();
		}

		public static void Serialize<T>(T objectToSerialize, TextWriter textWriter)
		{
			Serialize(objectToSerialize, textWriter, CreateDefaultContext(SerializationOptions.None));
		}
		public static void Serialize<T>(T objectToSerialize, TextWriter textWriter, SerializationOptions options)
		{
			Serialize(objectToSerialize, textWriter, CreateDefaultContext(options));
		}
		public static void Serialize<T>(T objectToSerialize, TextWriter textWriter, SerializationContext context)
		{
			if (textWriter == null) throw new ArgumentNullException("textWriter");
			if (context == null) throw new ArgumentNullException("context");

			if (objectToSerialize == null)
			{
				textWriter.Write("null");
				textWriter.Flush();
				return;
			}


			var writer = new JsonTextWriter(textWriter, context);
			writer.WriteValue(objectToSerialize, typeof(T));
			writer.Flush();
		}

		public static void Serialize<T>(T objectToSerialize, IJsonWriter writer, SerializationContext context)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (context == null) throw new ArgumentNullException("context");

			if (objectToSerialize == null)
			{
				writer.WriteNull();
				writer.Flush();
				return;
			}

			writer.WriteValue(objectToSerialize, typeof(T));
			writer.Flush();
		}

		public static string SerializeToString<T>(T objectToSerialize)
		{
			return SerializeToString(objectToSerialize, CreateDefaultContext(SerializationOptions.None));
		}
		public static string SerializeToString<T>(T objectToSerialize, SerializationOptions options)
		{
			return SerializeToString(objectToSerialize, CreateDefaultContext(options));
		}
		public static string SerializeToString<T>(T objectToSerialize, SerializationContext context)
		{
			if (context == null) throw new ArgumentNullException("context");

			if (objectToSerialize == null)
				return "null";

			var writer = new JsonStringBuilderWriter(new StringBuilder(), context);
			writer.WriteValue(objectToSerialize, typeof(T));
			writer.Flush();

			return writer.ToString();
		}

		public static object Deserialize(Type objectType, Stream jsonStream)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(SerializationOptions.None));
		}
		public static object Deserialize(Type objectType, Stream jsonStream, Encoding encoding)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(SerializationOptions.None, encoding));
		}
		public static object Deserialize(Type objectType, Stream jsonStream, SerializationOptions options)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(options));
		}
		public static object Deserialize(Type objectType, Stream jsonStream, SerializationOptions options, Encoding encoding)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(options, encoding));
		}
		public static object Deserialize(Type objectType, Stream jsonStream, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (jsonStream == null) throw new ArgumentNullException("jsonStream");
			if (context == null) throw new ArgumentNullException("context");
			if (!jsonStream.CanRead) throw JsonSerializationException.StreamIsNotReadable();

			var reader = new JsonStreamReader(jsonStream, context);
			return reader.ReadValue(objectType, false);
		}

		public static object Deserialize(Type objectType, TextReader textReader)
		{
			return Deserialize(objectType, textReader, CreateDefaultContext(SerializationOptions.None));
		}
		public static object Deserialize(Type objectType, TextReader textReader, SerializationOptions options)
		{
			return Deserialize(objectType, textReader, CreateDefaultContext(options));
		}
		public static object Deserialize(Type objectType, TextReader textReader, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (textReader == null) throw new ArgumentNullException("textReader");
			if (context == null) throw new ArgumentNullException("context");

			var reader = new JsonTextReader(textReader, context);
			return reader.ReadValue(objectType, false);
		}

		public static object Deserialize(Type objectType, string jsonString)
		{
			return Deserialize(objectType, jsonString, CreateDefaultContext(SerializationOptions.None));
		}
		public static object Deserialize(Type objectType, string jsonString, SerializationOptions options)
		{
			return Deserialize(objectType, jsonString, CreateDefaultContext(options));
		}
		public static object Deserialize(Type objectType, string jsonString, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (jsonString == null) throw new ArgumentNullException("jsonString");
			if (context == null) throw new ArgumentNullException("context");


			var reader = new JsonStringReader(jsonString, context);
			return reader.ReadValue(objectType, false);
		}

		public static object Deserialize(Type objectType, IJsonReader reader)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (reader == null) throw new ArgumentNullException("reader");

			return reader.ReadValue(objectType, false);
		}

		public static T Deserialize<T>(Stream jsonStream)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(SerializationOptions.None));
		}
		public static T Deserialize<T>(Stream jsonStream, Encoding encoding)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(SerializationOptions.None, encoding));
		}
		public static T Deserialize<T>(Stream jsonStream, SerializationOptions options)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(options));
		}
		public static T Deserialize<T>(Stream jsonStream, SerializationOptions options, Encoding encoding)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(options, encoding));
		}
		public static T Deserialize<T>(Stream jsonStream, SerializationContext context)
		{
			return (T)Deserialize(typeof(T), jsonStream, context);

		}

		public static T Deserialize<T>(TextReader textReader)
		{
			return (T)Deserialize(typeof(T), textReader, CreateDefaultContext(SerializationOptions.None));
		}
		public static T Deserialize<T>(TextReader textReader, SerializationOptions options)
		{
			return (T)Deserialize(typeof(T), textReader, CreateDefaultContext(options));
		}
		public static T Deserialize<T>(TextReader textReader, SerializationContext context)
		{
			return (T)Deserialize(typeof(T), textReader, context);
		}

		public static T Deserialize<T>(string jsonString)
		{
			return (T)Deserialize(typeof(T), jsonString, CreateDefaultContext(SerializationOptions.None));
		}
		public static T Deserialize<T>(string jsonString, SerializationOptions options)
		{
			return (T)Deserialize(typeof(T), jsonString, CreateDefaultContext(options));
		}
		public static T Deserialize<T>(string jsonString, SerializationContext context)
		{
			return (T)Deserialize(typeof(T), jsonString, context);
		}

		public static T Deserialize<T>(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var serializer = reader.Context.GetSerializerForType(typeof(T));
			return (T)serializer.Deserialize(reader);
		}

		private static SerializationContext CreateDefaultContext(SerializationOptions options, Encoding encoding = null)
		{
			return new SerializationContext
			{
				Encoding = encoding ?? DefaultEncoding,
				Options = options
			};
		}
	}
}

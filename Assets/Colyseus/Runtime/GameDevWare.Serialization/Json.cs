/*
	Copyright (c) 2026 Denis Zykov, GameDevWare.com

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
	/// <summary>
	/// Provides methods for serializing and deserializing objects to and from JSON format.
	/// </summary>
	public static class Json
	{

		private static IFormatProvider _DefaultFormat = CultureInfo.InvariantCulture;
		private static Encoding _DefaultEncoding = new UTF8Encoding(false, true);
		private static string[] _DefaultDateTimeFormats;

		/// <summary>
		/// Gets or sets the default date time formats used for parsing dates.
		/// </summary>
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
		/// <summary>
		/// Gets or sets the default format provider used for numeric and date formatting.
		/// </summary>
		public static IFormatProvider DefaultFormat
		{
			get { return _DefaultFormat; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");

				_DefaultFormat = value;
			}
		}
		/// <summary>
		/// Gets or sets the default encoding used for serialization and deserialization.
		/// </summary>
		public static Encoding DefaultEncoding
		{
			get { return _DefaultEncoding; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");

				_DefaultEncoding = value;
			}
		}
		/// <summary>
		/// Gets the list of default type serializers.
		/// </summary>
		public static List<TypeSerializer> DefaultSerializers { get; private set; }

		static Json()
		{
			// ReSharper disable StringLiteralTypo
			_DefaultDateTimeFormats = new[]
			{
				"yyyy-MM-ddTHH:mm:ss.fffffffK", // ISO 8601, full precision, kind-aware
				"yyyy-MM-ddTHH:mm:ss.fffffffZ", // ISO 8601, without timezone, full precision
				"o", // ISO 8601 roundtrip
				"yyyy-MM-ddTHH:mm:ss.fffzzz", // ISO 8601, with timezone
				"yyyy-MM-ddTHH:mm:ss.ffzzz", // ISO 8601, with timezone
				"yyyy-MM-ddTHH:mm:ss.fzzz", // ISO 8601, with timezone
				"yyyy-MM-ddTHH:mm:ssZ", // also ISO 8601, without timezone and without microseconds
				"yyyy-MM-ddTHH:mm:ss.fZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.ffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.fffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.ffffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.fffffZ", // also ISO 8601, without timezone
				"yyyy-MM-ddTHH:mm:ss.ffffffZ" // also ISO 8601, without timezone
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

#if UNITY_5 || UNITY_4 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_5_3_OR_NEWER
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

		/// <summary>
		/// Serializes the specified object to a JSON stream.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="jsonOutput">The stream to write the JSON data to.</param>
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Serializes the specified object to a JSON stream using the specified encoding.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="jsonOutput">The stream to write the JSON data to.</param>
		/// <param name="encoding">The encoding to use.</param>
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, Encoding encoding)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(SerializationOptions.None, encoding));
		}
		/// <summary>
		/// Serializes the specified object to a JSON stream using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="jsonOutput">The stream to write the JSON data to.</param>
		/// <param name="options">The serialization options.</param>
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, SerializationOptions options)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(options));
		}
		/// <summary>
		/// Serializes the specified object to a JSON stream using the specified options and encoding.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="jsonOutput">The stream to write the JSON data to.</param>
		/// <param name="options">The serialization options.</param>
		/// <param name="encoding">The encoding to use.</param>
		public static void Serialize<T>(T objectToSerialize, Stream jsonOutput, SerializationOptions options, Encoding encoding)
		{
			Serialize(objectToSerialize, jsonOutput, CreateDefaultContext(options, encoding));
		}
		/// <summary>
		/// Serializes the specified object to a JSON stream using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="jsonOutput">The stream to write the JSON data to.</param>
		/// <param name="context">The serialization context.</param>
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

		/// <summary>
		/// Serializes the specified object to a <see cref="TextWriter"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="textWriter">The <see cref="TextWriter"/> to write the JSON data to.</param>
		public static void Serialize<T>(T objectToSerialize, TextWriter textWriter)
		{
			Serialize(objectToSerialize, textWriter, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Serializes the specified object to a <see cref="TextWriter"/> using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="textWriter">The <see cref="TextWriter"/> to write the JSON data to.</param>
		/// <param name="options">The serialization options.</param>
		public static void Serialize<T>(T objectToSerialize, TextWriter textWriter, SerializationOptions options)
		{
			Serialize(objectToSerialize, textWriter, CreateDefaultContext(options));
		}
		/// <summary>
		/// Serializes the specified object to a <see cref="TextWriter"/> using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="textWriter">The <see cref="TextWriter"/> to write the JSON data to.</param>
		/// <param name="context">The serialization context.</param>
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

		/// <summary>
		/// Serializes the specified object using the specified <see cref="IJsonWriter"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="writer">The <see cref="IJsonWriter"/> to use for serialization.</param>
		/// <param name="context">The serialization context.</param>
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

		/// <summary>
		/// Serializes the specified object to a JSON string.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <returns>A JSON string representing the object.</returns>
		public static string SerializeToString<T>(T objectToSerialize)
		{
			return SerializeToString(objectToSerialize, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Serializes the specified object to a JSON string using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>A JSON string representing the object.</returns>
		public static string SerializeToString<T>(T objectToSerialize, SerializationOptions options)
		{
			return SerializeToString(objectToSerialize, CreateDefaultContext(options));
		}
		/// <summary>
		/// Serializes the specified object to a JSON string using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>A JSON string representing the object.</returns>
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

		/// <summary>
		/// Deserializes JSON data from a byte array to an object of the specified type.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] jsonBytes, int offset, int length)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize(objectType, new MemoryStream(jsonBytes, offset, length));
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of the specified type using the specified encoding.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] jsonBytes, int offset, int length, Encoding encoding)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize(objectType, new MemoryStream(jsonBytes, offset, length), encoding);
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of the specified type using the specified options.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] jsonBytes, int offset, int length, SerializationOptions options)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize(objectType, new MemoryStream(jsonBytes, offset, length), options);
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of the specified type using the specified options and encoding.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="options">The serialization options.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] jsonBytes, int offset, int length, SerializationOptions options, Encoding encoding)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize(objectType, new MemoryStream(jsonBytes, offset, length), options, encoding);
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of the specified type using the specified context.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] jsonBytes, int offset, int length, SerializationContext context)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize(objectType, new MemoryStream(jsonBytes, offset, length), context);
		}

		/// <summary>
		/// Deserializes JSON data from a stream to an object of the specified type.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream jsonStream)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of the specified type using the specified encoding.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream jsonStream, Encoding encoding)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(SerializationOptions.None, encoding));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of the specified type using the specified options.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream jsonStream, SerializationOptions options)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of the specified type using the specified options and encoding.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream jsonStream, SerializationOptions options, Encoding encoding)
		{
			return Deserialize(objectType, jsonStream, CreateDefaultContext(options, encoding));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of the specified type using the specified context.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream jsonStream, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (jsonStream == null) throw new ArgumentNullException("jsonStream");
			if (context == null) throw new ArgumentNullException("context");
			if (!jsonStream.CanRead) throw JsonSerializationException.StreamIsNotReadable();

			var reader = new JsonStreamReader(jsonStream, context);
			return reader.ReadValue(objectType, false);
		}

		/// <summary>
		/// Deserializes JSON data from a <see cref="TextReader"/> to an object of the specified type.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="textReader">The <see cref="TextReader"/> containing the JSON data.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, TextReader textReader)
		{
			return Deserialize(objectType, textReader, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes JSON data from a <see cref="TextReader"/> to an object of the specified type using the specified options.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="textReader">The <see cref="TextReader"/> containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, TextReader textReader, SerializationOptions options)
		{
			return Deserialize(objectType, textReader, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes JSON data from a <see cref="TextReader"/> to an object of the specified type using the specified context.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="textReader">The <see cref="TextReader"/> containing the JSON data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, TextReader textReader, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (textReader == null) throw new ArgumentNullException("textReader");
			if (context == null) throw new ArgumentNullException("context");

			var reader = new JsonTextReader(textReader, context);
			return reader.ReadValue(objectType, false);
		}

		/// <summary>
		/// Deserializes JSON data from a string to an object of the specified type.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonString">The string containing the JSON data.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, string jsonString)
		{
			return Deserialize(objectType, jsonString, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes JSON data from a string to an object of the specified type using the specified options.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonString">The string containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, string jsonString, SerializationOptions options)
		{
			return Deserialize(objectType, jsonString, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes JSON data from a string to an object of the specified type using the specified context.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="jsonString">The string containing the JSON data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, string jsonString, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (jsonString == null) throw new ArgumentNullException("jsonString");
			if (context == null) throw new ArgumentNullException("context");


			var reader = new JsonStringReader(jsonString, context);
			return reader.ReadValue(objectType, false);
		}

		/// <summary>
		/// Deserializes JSON data from a <see cref="IJsonReader"/> to an object of the specified type.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="reader">The <see cref="IJsonReader"/> to read the JSON data from.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, IJsonReader reader)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (reader == null) throw new ArgumentNullException("reader");

			return reader.ReadValue(objectType, false);
		}


		/// <summary>
		/// Deserializes JSON data from a byte array to an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] jsonBytes, int offset, int length)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize<T>(new MemoryStream(jsonBytes, offset, length));
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of type <typeparamref name="T"/> using the specified encoding.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] jsonBytes, int offset, int length, Encoding encoding)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize<T>(new MemoryStream(jsonBytes, offset, length), encoding);
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of type <typeparamref name="T"/> using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] jsonBytes, int offset, int length, SerializationOptions options)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize<T>(new MemoryStream(jsonBytes, offset, length), options);
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of type <typeparamref name="T"/> using the specified options and encoding.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="options">The serialization options.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] jsonBytes, int offset, int length, SerializationOptions options, Encoding encoding)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize<T>(new MemoryStream(jsonBytes, offset, length), options, encoding);
		}
		/// <summary>
		/// Deserializes JSON data from a byte array to an object of type <typeparamref name="T"/> using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonBytes">The byte array containing the JSON data.</param>
		/// <param name="offset">The starting offset in the byte array.</param>
		/// <param name="length">The number of bytes to read.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] jsonBytes, int offset, int length, SerializationContext context)
		{
			if (jsonBytes == null) throw new ArgumentNullException("jsonBytes");

			return Deserialize<T>( new MemoryStream(jsonBytes, offset, length), context);
		}


		/// <summary>
		/// Deserializes JSON data from a stream to an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream jsonStream)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of type <typeparamref name="T"/> using the specified encoding.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream jsonStream, Encoding encoding)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(SerializationOptions.None, encoding));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of type <typeparamref name="T"/> using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream jsonStream, SerializationOptions options)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of type <typeparamref name="T"/> using the specified options and encoding.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <param name="encoding">The encoding to use.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream jsonStream, SerializationOptions options, Encoding encoding)
		{
			return (T)Deserialize(typeof(T), jsonStream, CreateDefaultContext(options, encoding));
		}
		/// <summary>
		/// Deserializes JSON data from a stream to an object of type <typeparamref name="T"/> using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonStream">The stream containing the JSON data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream jsonStream, SerializationContext context)
		{
			return (T)Deserialize(typeof(T), jsonStream, context);

		}

		/// <summary>
		/// Deserializes JSON data from a <see cref="TextReader"/> to an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="textReader">The <see cref="TextReader"/> containing the JSON data.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(TextReader textReader)
		{
			return (T)Deserialize(typeof(T), textReader, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes JSON data from a <see cref="TextReader"/> to an object of type <typeparamref name="T"/> using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="textReader">The <see cref="TextReader"/> containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(TextReader textReader, SerializationOptions options)
		{
			return (T)Deserialize(typeof(T), textReader, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes JSON data from a <see cref="TextReader"/> to an object of type <typeparamref name="T"/> using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="textReader">The <see cref="TextReader"/> containing the JSON data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(TextReader textReader, SerializationContext context)
		{
			return (T)Deserialize(typeof(T), textReader, context);
		}

		/// <summary>
		/// Deserializes JSON data from a string to an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonString">The string containing the JSON data.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(string jsonString)
		{
			return (T)Deserialize(typeof(T), jsonString, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes JSON data from a string to an object of type <typeparamref name="T"/> using the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonString">The string containing the JSON data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>A JSON string representing the object.</returns>
		public static T Deserialize<T>(string jsonString, SerializationOptions options)
		{
			return (T)Deserialize(typeof(T), jsonString, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes JSON data from a string to an object of type <typeparamref name="T"/> using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="jsonString">The string containing the JSON data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(string jsonString, SerializationContext context)
		{
			return (T)Deserialize(typeof(T), jsonString, context);
		}

		/// <summary>
		/// Deserializes JSON data from a <see cref="IJsonReader"/> to an object of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="reader">The <see cref="IJsonReader"/> to read the JSON data from.</param>
		/// <returns>The deserialized object.</returns>
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

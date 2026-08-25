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
using System.IO;
using System.Text;
using GameDevWare.Serialization.MessagePack;
using GameDevWare.Serialization.Serializers;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Provides methods for MessagePack serialization and deserialization.
	/// </summary>
	public static class MsgPack
	{
		/// <summary>
		/// Gets or sets the default date and time formats used during serialization and deserialization.
		/// </summary>
		public static string[] DefaultDateTimeFormats { get { return Json.DefaultDateTimeFormats; } set { Json.DefaultDateTimeFormats = value; } }
		/// <summary>
		/// Gets or sets the default format provider used during serialization and deserialization.
		/// </summary>
		public static IFormatProvider DefaultFormat { get { return Json.DefaultFormat; } set { Json.DefaultFormat = value; } }
		/// <summary>
		/// Gets or sets the default encoding used during serialization and deserialization.
		/// </summary>
		public static Encoding DefaultEncoding { get { return Json.DefaultEncoding; } set { Json.DefaultEncoding = value; } }
		/// <summary>
		/// Gets the list of default type serializers.
		/// </summary>
		public static List<TypeSerializer> DefaultSerializers { get { return Json.DefaultSerializers; } }
		/// <summary>
		/// Gets or sets the handler for MessagePack extension types.
		/// </summary>
		public static MessagePackExtensionTypeHandler ExtensionTypeHandler { get; private set; }

		static MsgPack()
		{
			ExtensionTypeHandler = new DefaultMessagePackExtensionTypeHandler(EndianBitConverter.Big);
		}

		/// <summary>
		/// Serializes the specified object to a MessagePack stream.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="msgPackOutput">The stream to which the MessagePack data will be written.</param>
		public static void Serialize<T>(T objectToSerialize, Stream msgPackOutput)
		{
			Serialize(objectToSerialize, msgPackOutput, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Serializes the specified object to a MessagePack stream with the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="msgPackOutput">The stream to which the MessagePack data will be written.</param>
		/// <param name="options">The serialization options.</param>
		public static void Serialize<T>(T objectToSerialize, Stream msgPackOutput, SerializationOptions options)
		{
			Serialize(objectToSerialize, msgPackOutput, CreateDefaultContext(options));
		}
		/// <summary>
		/// Serializes the specified object to a MessagePack stream using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to serialize.</typeparam>
		/// <param name="objectToSerialize">The object to serialize.</param>
		/// <param name="msgPackOutput">The stream to which the MessagePack data will be written.</param>
		/// <param name="context">The serialization context.</param>
		public static void Serialize<T>(T objectToSerialize, Stream msgPackOutput, SerializationContext context)
		{
			if (msgPackOutput == null) throw new ArgumentNullException("msgPackOutput");
			if (context == null) throw new ArgumentNullException("context");
			if (!msgPackOutput.CanWrite) throw JsonSerializationException.StreamIsNotWriteable();

			var writer = new MsgPackWriter(msgPackOutput, context);
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
		/// Deserializes the MessagePack data from the specified byte array.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="msgPackInput">The byte array containing the MessagePack data.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="msgPackInput"/> at which to begin reading.</param>
		/// <param name="length">The number of bytes to read from <paramref name="msgPackInput"/>.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] msgPackInput, int offset, int length)
		{
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");

			return Deserialize(objectType, new MemoryStream(msgPackInput, offset, length));
		}
		/// <summary>
		/// Deserializes the MessagePack data from the specified byte array with the specified options.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="msgPackInput">The byte array containing the MessagePack data.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="msgPackInput"/> at which to begin reading.</param>
		/// <param name="length">The number of bytes to read from <paramref name="msgPackInput"/>.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] msgPackInput, int offset, int length, SerializationOptions options)
		{
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");

			return Deserialize(objectType, new MemoryStream(msgPackInput, offset, length), options);
		}
		/// <summary>
		/// Deserializes the MessagePack data from the specified byte array using the specified context.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="msgPackInput">The byte array containing the MessagePack data.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="msgPackInput"/> at which to begin reading.</param>
		/// <param name="length">The number of bytes to read from <paramref name="msgPackInput"/>.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, byte[] msgPackInput, int offset, int length, SerializationContext context)
		{
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");

			return Deserialize(objectType, new MemoryStream(msgPackInput, offset, length), context);
		}

		/// <summary>
		/// Deserializes the MessagePack data from the specified stream.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="msgPackInput">The stream containing the MessagePack data.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream msgPackInput)
		{
			return Deserialize(objectType, msgPackInput, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes the MessagePack data from the specified stream with the specified options.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="msgPackInput">The stream containing the MessagePack data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream msgPackInput, SerializationOptions options)
		{
			return Deserialize(objectType, msgPackInput, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes the MessagePack data from the specified stream using the specified context.
		/// </summary>
		/// <param name="objectType">The type of the object to deserialize.</param>
		/// <param name="msgPackInput">The stream containing the MessagePack data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static object Deserialize(Type objectType, Stream msgPackInput, SerializationContext context)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");
			if (context == null) throw new ArgumentNullException("context");
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");
			if (!msgPackInput.CanRead) throw JsonSerializationException.StreamIsNotReadable();

			var reader = new MsgPackReader(msgPackInput, context);
			return reader.ReadValue(objectType, false);
		}

		/// <summary>
		/// Deserializes the MessagePack data of the specified type from the specified byte array.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="msgPackInput">The byte array containing the MessagePack data.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="msgPackInput"/> at which to begin reading.</param>
		/// <param name="length">The number of bytes to read from <paramref name="msgPackInput"/>.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] msgPackInput, int offset, int length)
		{
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");

			return Deserialize<T>(new MemoryStream(msgPackInput, offset, length));
		}
		/// <summary>
		/// Deserializes the MessagePack data of the specified type from the specified byte array with the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="msgPackInput">The byte array containing the MessagePack data.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="msgPackInput"/> at which to begin reading.</param>
		/// <param name="length">The number of bytes to read from <paramref name="msgPackInput"/>.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] msgPackInput, int offset, int length, SerializationOptions options)
		{
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");

			return Deserialize<T>(new MemoryStream(msgPackInput, offset, length), options);
		}
		/// <summary>
		/// Deserializes the MessagePack data of the specified type from the specified byte array using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="msgPackInput">The byte array containing the MessagePack data.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="msgPackInput"/> at which to begin reading.</param>
		/// <param name="length">The number of bytes to read from <paramref name="msgPackInput"/>.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(byte[] msgPackInput, int offset, int length, SerializationContext context)
		{
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");

			return Deserialize<T>(new MemoryStream(msgPackInput, offset, length), context);
		}

		/// <summary>
		/// Deserializes the MessagePack data of the specified type from the specified stream.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="msgPackInput">The stream containing the MessagePack data.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream msgPackInput)
		{
			return Deserialize<T>(msgPackInput, CreateDefaultContext(SerializationOptions.None));
		}
		/// <summary>
		/// Deserializes the MessagePack data of the specified type from the specified stream with the specified options.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="msgPackInput">The stream containing the MessagePack data.</param>
		/// <param name="options">The serialization options.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream msgPackInput, SerializationOptions options)
		{
			return Deserialize<T>(msgPackInput, CreateDefaultContext(options));
		}
		/// <summary>
		/// Deserializes the MessagePack data of the specified type from the specified stream using the specified context.
		/// </summary>
		/// <typeparam name="T">The type of the object to deserialize.</typeparam>
		/// <param name="msgPackInput">The stream containing the MessagePack data.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>The deserialized object.</returns>
		public static T Deserialize<T>(Stream msgPackInput, SerializationContext context)
		{
			if (context == null) throw new ArgumentNullException("context");
			if (msgPackInput == null) throw new ArgumentNullException("msgPackInput");
			if (!msgPackInput.CanRead) throw JsonSerializationException.StreamIsNotReadable();

			return (T)Deserialize(typeof(T), msgPackInput, context);
		}

		private static SerializationContext CreateDefaultContext(SerializationOptions options)
		{
			return new SerializationContext
			{
				Options = options,
				EnumSerializerFactory = (enumType) => new EnumNumberSerializer(enumType)
			};
		}
	}
}

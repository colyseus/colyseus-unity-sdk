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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using GameDevWare.Serialization.MessagePack;
using GameDevWare.Serialization.Serializers;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Represents the context for serialization and deserialization operations, containing configuration and state.
	/// </summary>
	public sealed class SerializationContext
	{
		private readonly Dictionary<Type, TypeSerializer> serializers;
		private MessagePackExtensionTypeHandler extensionTypeHandler;

		/// <summary>
		/// Gets the current hierarchy of objects being serialized or deserialized.
		/// </summary>
		public Stack<object> Hierarchy { get; private set; }
		/// <summary>
		/// Gets the current path in the object graph.
		/// </summary>
		public Stack<PathSegment> Path { get; private set; }

		/// <summary>
		/// Limits the recursion depth for object graphs to prevent stack overflow and mitigate potential Denial of Service (DoS) attacks.
		/// <para>While a higher limit allows for more complex, deeply nested data structures, it increases the risk of exhausting the stack;
		/// a tighter limit provides more safety but may prevent the serialization of valid, deeply nested graphs.</para>
		/// </summary>
		public int MaxHierarchyDepth { get; set; }
		/// <summary>
		/// Gets or sets the format provider for numeric and date-time values.
		/// </summary>
		public IFormatProvider Format { get; set; }
		/// <summary>
		/// Gets or sets the date-time formats for JSON.
		/// </summary>
		public string[] DateTimeFormats { get; set; }
		/// <summary>
		/// Gets or sets the encoding for text operations.
		/// </summary>
		public Encoding Encoding { get; set; }

		/// <summary>
		/// Gets or sets a dictionary of registered type serializers for explicit type handling.
		/// <para>Use this to override default serialization for specific types or to provide support for complex user-defined types.
		/// Registered serializers take precedence over default or factory-generated serializers.</para>
		/// </summary>
		public Dictionary<Type, TypeSerializer> Serializers
		{
			get { return this.serializers; }
			set
			{
				if (value == null) throw new ArgumentNullException("value");

				foreach (var kv in value)
					this.serializers[kv.Key] = kv.Value;
			}
		}
		/// <summary>
		/// Gets or sets the handler for MessagePack extension types (type codes -1 to -128).
		/// <para>Use this to provide high-performance binary serialization for types like <see cref="Guid"/>, <see cref="decimal"/>, or
		/// custom engine-specific structures that require a compact binary representation beyond standard objects or arrays.</para>
		/// </summary>
		public MessagePackExtensionTypeHandler ExtensionTypeHandler
		{
			get { return this.extensionTypeHandler; }
			set { if (value == null) throw new ArgumentNullException("value"); this.extensionTypeHandler = value; }
		}

		/// <summary>
		/// Gets or sets the serialization options.
		/// </summary>
		public SerializationOptions Options { get; set; }

		/// <summary>
		/// Gets or sets a factory for creating object serializers.
		/// <para>Use this to globally customize how classes and structs are handled, such as adding specialized validation logic,
		/// support for non-standard inheritance, or automatically wrapping instances in proxies during deserialization.</para>
		/// </summary>
		public Func<Type, TypeSerializer> ObjectSerializerFactory { get; set; }
		/// <summary>
		/// Gets or sets a factory for creating enum serializers.
		/// <para>Use this to customize enum handling, such as forcing all enums to serialize as strings instead of integers
		/// for better interoperability with external systems.</para>
		/// </summary>
		public Func<Type, TypeSerializer> EnumSerializerFactory { get; set; }
		/// <summary>
		/// Gets or sets a factory for creating dictionary serializers.
		/// <para>Use this to support non-standard dictionary implementations or to enforce specific key-sorting or filtering
		/// logic across all dictionary-like structures.</para>
		/// </summary>
		public Func<Type, TypeSerializer> DictionarySerializerFactory { get; set; }
		/// <summary>
		/// Gets or sets a factory for creating array and collection serializers.
		/// <para>Use this to provide specialized support for custom collection types or to implement global
		/// constraints on collection sizes and element types.</para>
		/// </summary>
		public Func<Type, TypeSerializer> ArraySerializerFactory { get; set; }
		/// <summary>
		/// Gets or sets a general-purpose factory for resolving serializers for any type.
		/// <para>This factory acts as the primary resolution point before falling back to specialized factories.
		/// Use it to implement complex, type-agnostic serialization strategies or to integrate with external DI containers.</para>
		/// </summary>
		public Func<Type, TypeSerializer> SerializerFactory { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SerializationContext"/> class.
		/// </summary>
		public SerializationContext()
		{
			this.Hierarchy = new Stack<object>();
			this.Path = new Stack<PathSegment>();

			this.MaxHierarchyDepth = 64;
			this.Format = Json.DefaultFormat;
			this.DateTimeFormats = Json.DefaultDateTimeFormats;
			this.Encoding = Json.DefaultEncoding;
			this.ExtensionTypeHandler = MsgPack.ExtensionTypeHandler;

			this.serializers = Json.DefaultSerializers.ToDictionary(s => s.SerializedType);
		}

		/// <summary>
		/// Gets or creates a serializer for the specified type.
		/// </summary>
		/// <param name="valueType">The type to get a serializer for.</param>
		/// <returns>A <see cref="TypeSerializer"/> instance.</returns>
		public TypeSerializer GetSerializerForType(Type valueType)
		{
			if (valueType == null) throw new ArgumentNullException("valueType");

			if (valueType.BaseType == typeof(MulticastDelegate) || valueType.BaseType == typeof(Delegate))
				throw JsonSerializationException.CantSerializeDelegateType(valueType);

			var serializer = default(TypeSerializer);
			if (this.serializers.TryGetValue(valueType, out serializer))
			{
				if (serializer == null)
					return null; // recursion during creation

				return serializer;
			}

			// prevent infinite recursion during creation
			this.serializers.Add(valueType, null);
			try
			{
				var typeSerializerAttribute = valueType.GetCustomAttributes(typeof(TypeSerializerAttribute), inherit: false).FirstOrDefault() as TypeSerializerAttribute;
				if (typeSerializerAttribute != null)
					serializer = this.CreateCustomSerializer(valueType, typeSerializerAttribute);
				else if (valueType.IsEnum)
					serializer = this.CreateEnumSerializer(valueType);
				else if (typeof(IDictionary).IsAssignableFrom(valueType) || valueType.IsInstantiationOf(typeof(IDictionary<,>)))
					serializer = this.CreateDictionarySerializer(valueType);
				else if (valueType.IsArray || typeof(IEnumerable).IsAssignableFrom(valueType))
					serializer = this.CreateArraySerializer(valueType);
				else
					serializer = (this.SerializerFactory != null ? this.SerializerFactory(valueType) : null) ?? this.CreateObjectSerializer(valueType);

				this.serializers[valueType] = serializer;
				return serializer;
			}
			catch
			{
				this.serializers.Remove(valueType);
				throw;
			}
		}

		private TypeSerializer CreateDictionarySerializer(Type valueType)
		{
			if (this.DictionarySerializerFactory != null)
				return this.DictionarySerializerFactory(valueType);
			else
				return new DictionarySerializer(valueType);
		}
		private TypeSerializer CreateEnumSerializer(Type valueType)
		{
			if (this.EnumSerializerFactory != null)
				return this.EnumSerializerFactory(valueType);
			else
				return new EnumSerializer(valueType);
		}
		private TypeSerializer CreateArraySerializer(Type valueType)
		{
			if (this.ArraySerializerFactory != null)
				return this.ArraySerializerFactory(valueType);
			else
				return new ArraySerializer(valueType);
		}
		private TypeSerializer CreateObjectSerializer(Type valueType)
		{
			if (this.ObjectSerializerFactory != null)
				return this.ObjectSerializerFactory(valueType);
			else
				return new ObjectSerializer(this, valueType);
		}
		private TypeSerializer CreateCustomSerializer(Type valueType, TypeSerializerAttribute typeSerializerAttribute)
		{
			var serializerType = typeSerializerAttribute.SerializerType;

			var typeCtr = serializerType.GetConstructor(new[] { typeof(Type) });
			if (typeCtr != null)
				return (TypeSerializer)typeCtr.Invoke(new object[] { valueType });

			var ctxTypeCtr = serializerType.GetConstructor(new[] { typeof(SerializationContext), typeof(Type) });
			if (ctxTypeCtr != null)
				return (TypeSerializer)ctxTypeCtr.Invoke(new object[] { this, valueType });

			var ctxCtr = serializerType.GetConstructor(new[] { typeof(SerializationContext) });
			if (ctxCtr != null)
				return (TypeSerializer)ctxCtr.Invoke(new object[] { this });

			return (TypeSerializer)Activator.CreateInstance(serializerType);
		}

		/// <summary>
		/// Gets the <see cref="Type"/> with the specified name, optionally throwing an exception on error and ignoring case.
		/// </summary>
		/// <param name="name">The name of the type to get.</param>
		/// <param name="throwOnError">True to throw an exception if the type cannot be found; otherwise, false.</param>
		/// <param name="ignoreCase">True to ignore the case of the type name; otherwise, false.</param>
		/// <returns>The <see cref="Type"/> with the specified name, or null if the type is not found.</returns>
		public Type GetType(string name, bool throwOnError, bool ignoreCase)
		{
			return Type.GetType(name, throwOnError, ignoreCase);
		}
		/// <summary>
		/// Gets the <see cref="Type"/> with the specified name, optionally throwing an exception on error.
		/// </summary>
		/// <param name="name">The name of the type to get.</param>
		/// <param name="throwOnError">True to throw an exception if the type cannot be found; otherwise, false.</param>
		/// <returns>The <see cref="Type"/> with the specified name, or null if the type is not found.</returns>
		public Type GetType(string name, bool throwOnError)
		{
			return Type.GetType(name, throwOnError);
		}
		/// <summary>
		/// Gets the <see cref="Type"/> with the specified name.
		/// </summary>
		/// <param name="name">The name of the type to get.</param>
		/// <returns>The <see cref="Type"/> with the specified name, or null if the type is not found.</returns>
		public Type GetType(string name)
		{
			return Type.GetType(name);
		}

		/// <summary>
		/// Reset serialization context for future re-use. Clears <see cref="Hierarchy"/> and <see cref="Path"/> collections.
		/// </summary>
		public void Reset()
		{
			this.Hierarchy.Clear();
			this.Path.Clear();
		}

		/// <summary>
		/// Get object hierarchy (arrays/objects) path to current reader position.
		/// </summary>
		/// <returns></returns>
		public string GetPath()
		{
			var path = new StringBuilder();
			foreach (var segment in this.Path.Reverse())
			{
				var segmentString = segment.ToString();
				if (string.IsNullOrEmpty(segmentString))
				{
					continue;
				}
				path.Append(segmentString);
				path.Append(".");
			}

			if (path.Length > 0)
			{
				path.Length--;
			}

			return path.ToString();
		}
	}
}

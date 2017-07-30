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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GameDevWare.Serialization.Serializers;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public sealed class SerializationContext
	{
		private readonly Dictionary<Type, TypeSerializer> serializers;

		public Stack Hierarchy { get; private set; }

		public IFormatProvider Format { get; set; }
		public string[] DateTimeFormats { get; set; }
		public Encoding Encoding { get; set; }

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

		public SerializationOptions Options { get; set; }

		public Func<Type, TypeSerializer> ObjectSerializerFactory { get; set; }
		public Func<Type, TypeSerializer> EnumSerializerFactory { get; set; }
		public Func<Type, TypeSerializer> DictionarySerializerFactory { get; set; }
		public Func<Type, TypeSerializer> ArraySerializerFactory { get; set; }
		public Func<Type, TypeSerializer> SerializerFactory { get; set; }

		public SerializationContext()
		{
			this.Hierarchy = new Stack();

			this.Format = Json.DefaultFormat;
			this.DateTimeFormats = Json.DefaultDateTimeFormats;
			this.Encoding = Json.DefaultEncoding;
			this.serializers = Json.DefaultSerializers.ToDictionary(s => s.SerializedType);
		}

		public TypeSerializer GetSerializerForType(Type valueType)
		{
			if (valueType == null) throw new ArgumentNullException("valueType");

			if (valueType.BaseType == typeof(MulticastDelegate) || valueType.BaseType == typeof(Delegate))
				throw new InvalidOperationException(string.Format("Unable to serialize delegate type '{0}'.", valueType));

			var serializer = default(TypeSerializer);
			if (this.serializers.TryGetValue(valueType, out serializer))
				return serializer;

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

			this.serializers.Add(valueType, serializer);
			return serializer;
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

		public Type GetType(string name, bool throwOnError, bool ignoreCase)
		{
			return Type.GetType(name, throwOnError, ignoreCase);
		}
		public Type GetType(string name, bool throwOnError)
		{
			return Type.GetType(name, throwOnError);
		}
		public Type GetType(string name)
		{
			return Type.GetType(name);
		}

		#region NotSupported

		public Assembly GetAssembly(AssemblyName name, bool throwOnError)
		{
			throw new NotSupportedException();
		}

		public Assembly GetAssembly(AssemblyName name)
		{
			throw new NotSupportedException();
		}

		public string GetPathOfAssembly(AssemblyName name)
		{
			throw new NotSupportedException();
		}

		public void ReferenceAssembly(AssemblyName name)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}

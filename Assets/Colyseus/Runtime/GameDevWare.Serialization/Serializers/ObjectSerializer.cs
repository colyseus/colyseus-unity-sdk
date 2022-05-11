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
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using GameDevWare.Serialization.Metadata;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public class ObjectSerializer : TypeSerializer
	{
		public const string TYPE_MEMBER_NAME = "_type";

		private static readonly Regex VersionRegEx = new Regex(@", Version=[^\]]+", RegexOptions.None);
		private static readonly string BclTypePart = typeof(byte).AssemblyQualifiedName.Substring(typeof(byte).FullName.Length);

		private readonly Type objectType;
		private readonly string objectTypeNameWithoutVersion;
		private readonly TypeDescription objectTypeDescription;
		private readonly ObjectSerializer baseTypeSerializer;
		private readonly SerializationContext context;

		public override Type SerializedType { get { return this.objectType; } }

		public bool SuppressTypeInformation { get; set; }

		public ObjectSerializer(SerializationContext context, Type type)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (context == null) throw new ArgumentNullException("context");

			this.context = context;
			this.objectType = type;
			this.objectTypeNameWithoutVersion = GetVersionInvariantObjectTypeName(this.objectType);
			this.SuppressTypeInformation = (context.Options & SerializationOptions.SuppressTypeInformation) ==
										   SerializationOptions.SuppressTypeInformation;

			if (this.objectType.BaseType != null && this.objectType.BaseType != typeof(object))
			{
				var baseSerializer = context.GetSerializerForType(this.objectType.BaseType);
				if (baseSerializer is ObjectSerializer == false)
				{
					throw JsonSerializationException.TypeRequiresCustomSerializer(this.objectType, this.GetType());
				}
				this.baseTypeSerializer = (ObjectSerializer)baseSerializer;
			}

			this.objectTypeDescription = TypeDescription.Get(type);
		}

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token != JsonToken.BeginObject)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.BeginObject);

			var serializerOverride = default(ObjectSerializer);
			var container = new IndexedDictionary<string, object>(10);
			reader.Context.Hierarchy.Push(container);
			var instance = this.DeserializeMembers(reader, container, ref serializerOverride);
			reader.Context.Hierarchy.Pop();

			if (reader.Token != JsonToken.EndOfObject)
				throw JsonSerializationException.UnexpectedToken(reader, JsonToken.EndOfObject);

			if (instance != null)
				return instance;
			else if (serializerOverride != null)
				return serializerOverride.PopulateInstance(container, null);
			else
				return this.PopulateInstance(container, null);
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var container = new IndexedDictionary<DataMemberDescription, object>();

			this.CollectMemberValues(value, container);

			if (this.SuppressTypeInformation || this.objectTypeDescription.IsAnonymousType)
			{
				writer.WriteObjectBegin(container.Count);

			}
			else
			{
				writer.WriteObjectBegin(container.Count + 1);

				writer.Context.Path.Push(new PathSegment(TYPE_MEMBER_NAME));
				writer.WriteMember(TYPE_MEMBER_NAME);
				writer.WriteString(objectTypeNameWithoutVersion);
				this.context.Path.Pop();
			}

			foreach (var kv in container)
			{
				writer.Context.Path.Push(new PathSegment(kv.Key.Name));
				writer.WriteMember(kv.Key.Name);
				writer.WriteValue(kv.Value, kv.Key.ValueType);
				this.context.Path.Pop();
			}

			writer.WriteObjectEnd();
		}

		private void CollectMemberValues(object instance, IndexedDictionary<DataMemberDescription, object> container)
		{
			if (this.baseTypeSerializer != null)
				this.baseTypeSerializer.CollectMemberValues(instance, container);

			foreach (var member in this.objectTypeDescription.Members)
			{
				var baseMemberWithSameName = default(DataMemberDescription);
				if (this.baseTypeSerializer != null && this.baseTypeSerializer.TryGetMember(member.Name, out baseMemberWithSameName))
					container.Remove(baseMemberWithSameName);

				var value = member.GetValue(instance);

				container[member] = value;
			}
		}
		private object DeserializeMembers(IJsonReader reader, IndexedDictionary<string, object> container, ref ObjectSerializer serializerOverride)
		{
			while (reader.NextToken() && reader.Token != JsonToken.EndOfObject)
			{
				if (reader.Token != JsonToken.Member)
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.Member);

				string memberName = null;
				object value = null;

				memberName = reader.Value.AsString; // string
				if (string.Equals(memberName, TYPE_MEMBER_NAME) && this.SuppressTypeInformation == false)
				{
					this.context.Path.Push(new PathSegment(TYPE_MEMBER_NAME));
					reader.NextToken();
					var typeName = reader.ReadString(false);
					var type = default(Type);
					try
					{
						type = reader.Context.GetType(typeName, true, true);
					}
					catch (Exception getTypeError)
					{
						throw new SerializationException(string.Format("Failed to resolve type '{0}' of value for '{1}' of '{2}' type.\r\n" +
							"More detailed information in inner exception.", typeName, memberName, this.objectType.Name), getTypeError);
					}
					this.context.Path.Pop();

					if (type == typeof(object))
					{
						this.DeserializeMembers(reader, container, ref serializerOverride);
						return new object();
					}

					var serializer = reader.Context.GetSerializerForType(type);
					if (serializer is ObjectSerializer)
					{
						serializerOverride = (ObjectSerializer)serializer;
						serializerOverride.DeserializeMembers(reader, container, ref serializerOverride);
						return null;
					}
					else
					{
						reader.NextToken(); // nextToken to next member
						serializerOverride = null;
						return serializer.Deserialize(reader);
					}
				}

				this.context.Path.Push(new PathSegment(memberName));

				var member = default(DataMemberDescription);
				var valueType = typeof(object);

				if (this.TryGetMember(memberName, out member))
					valueType = member.ValueType;

				reader.NextToken();

				try
				{
					value = reader.ReadValue(valueType, false);
				}
				catch (Exception e)
				{
					throw new SerializationException(string.Format("Failed to read value for member '{0}' of '{1}' type.\r\nMore detailed information in inner exception.", memberName, this.objectType.Name), e);
				}

				container[memberName] = value;

				this.context.Path.Pop();
			}

			return null;
		}
		private object PopulateInstance(IndexedDictionary<string, object> container, object instance)
		{
			if (instance == null && objectType == typeof(object))
				return container;

			if (instance == null)
				instance = objectTypeDescription.CreateInstance();

			foreach (var member in this.objectTypeDescription.Members)
			{
				var memberName = member.Name;
				var memberType = member.ValueType;
				var defaultValue = member.DefaultValue;

				if (defaultValue == null || container.ContainsKey(memberName))
					continue;

				if (defaultValue.GetType() == memberType)
					container[memberName] = defaultValue;
				else if ("[]".Equals(defaultValue) || "{}".Equals(defaultValue))
					container[memberName] = memberType.IsArray
						? Array.CreateInstance(memberType.GetElementType(), 0)
						: Activator.CreateInstance(memberType);
				else if (defaultValue is string)
					container[memberName] = Json.Deserialize(memberType, (string)defaultValue, context);
				else
					container[memberName] = Convert.ChangeType(defaultValue, memberType, context.Format);
			}


			foreach (var kv in container)
			{
				var memberName = kv.Key;
				var value = kv.Value;
				var member = default(DataMemberDescription);

				if (!this.TryGetMember(memberName, out member))
					continue;

				try
				{
					member.SetValue(instance, value);
				}
				catch (Exception e)
				{
					throw new SerializationException(string.Format("Failed to set member '{0}' to value '{1}' of type {2}.\r\n More detailed information in inner exception.",
						memberName, value, value != null ? value.GetType().FullName : "<null>"), e);
				}
			}

			if (this.baseTypeSerializer != null)
				this.baseTypeSerializer.PopulateInstance(container, instance);

			return instance;
		}
		private bool TryGetMember(string memberName, out DataMemberDescription member)
		{
			if (memberName == null) throw new ArgumentNullException("memberName");

			if (this.objectTypeDescription.TryGetMember(memberName, out member))
				return true;

			if (this.baseTypeSerializer == null)
				return false;

			return this.baseTypeSerializer.TryGetMember(memberName, out member);
		}

		public static object CreateInstance(IndexedDictionary<string, object> values)
		{
			if (values == null) throw new ArgumentNullException("values");

			var instanceType = typeof(object);
			var instanceTypeName = default(object);
			if (values.TryGetValue(TYPE_MEMBER_NAME, out instanceTypeName))
			{
				values.Remove(TYPE_MEMBER_NAME);
				instanceType = Type.GetType((string)instanceTypeName, true);
			}
			return CreateInstance(values, instanceType);
		}
		public static object CreateInstance(IndexedDictionary<string, object> values, Type instanceType)
		{
			if (instanceType == null) throw new ArgumentNullException("instanceType");
			if (values == null) throw new ArgumentNullException("values");

			var context = new SerializationContext();
			var serializer = new ObjectSerializer(context, instanceType);
			return serializer.PopulateInstance(values, null);
		}
		public static string GetVersionInvariantObjectTypeName(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var fullName = (type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
			fullName = VersionRegEx.Replace(fullName, string.Empty);
			fullName = fullName.Replace(BclTypePart, ""); // remove BCL path of type information for better interop compatibility
			return fullName;
		}

		public override string ToString()
		{
			return string.Format("object, {0}", this.objectType);
		}
	}
}

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
using System.Linq;
using System.Text.RegularExpressions;
using GameDevWare.Serialization.Metadata;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	/// <summary>
	/// Serializer for object types.
	/// </summary>
	public class ObjectSerializer : TypeSerializer
	{
		/// <summary>
		/// The name of the member used to store polymorphic type metadata during serialization.
		/// <para>While allowing this metadata enables the reconstruction of complex inheritance hierarchies, it introduces a significant security risk.
		/// An attacker providing untrusted data can use this field to force the instantiation of arbitrary types, which may lead to Remote Code Execution (RCE).</para>
		/// </summary>
		public const string TYPE_MEMBER_NAME = "_type";

		private static readonly Regex VersionRegEx = new Regex(@", Version=[^\]]+", RegexOptions.None);
		private static readonly string BclTypePart = typeof(byte).AssemblyQualifiedName.Substring(typeof(byte).FullName.Length);

		private readonly Type objectType;
		private readonly string objectTypeNameWithoutVersion;
		private readonly TypeDescription objectTypeDescription;
		private readonly ObjectSerializer baseTypeSerializer;
		private readonly SerializationContext context;

		/// <inheritdoc />
		public override Type SerializedType { get { return this.objectType; } }

		/// <summary>
		/// Gets or sets a value indicating whether to suppress type information.
		/// </summary>
		public bool SuppressTypeInformation { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectSerializer"/> class.
		/// </summary>
		/// <param name="context">The serialization context.</param>
		/// <param name="type">The type of the object to serialize or deserialize.</param>
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

		/// <inheritdoc />
		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Token != JsonToken.BeginObject)
				throw JsonSerializationException.UnexpectedToken(reader, this.objectType, JsonToken.BeginObject);

			if (reader.Context.Hierarchy.Count >= reader.Context.MaxHierarchyDepth)
				throw JsonSerializationException.SerializationGraphIsTooDeep(reader, (ulong)reader.Context.MaxHierarchyDepth);

			var serializerOverride = default(ObjectSerializer);
			var container = new IndexedDictionary<string, object>(10);
			reader.Context.Hierarchy.Push(container);
			try
			{
				var instance = this.DeserializeMembers(reader, container, ref serializerOverride);

				if (reader.Token != JsonToken.EndOfObject)
				{
					if (reader.Token == JsonToken.EndOfStream)
						throw JsonSerializationException.UnexpectedEndOfStream(reader, "reading object members");

					throw JsonSerializationException.UnexpectedToken(reader, this.objectType, JsonToken.EndOfObject);
				}

				if (instance != null)
					return instance;
				else if (serializerOverride != null)
					return serializerOverride.PopulateInstance(container, null);
				else
					return this.PopulateInstance(container, null);
			}
			finally
			{
				reader.Context.Hierarchy.Pop();
			}
		}

		/// <inheritdoc />
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			if (writer.Context.Hierarchy.Contains(value, IdentityComparer.Default))
				throw JsonSerializationException.CircularReferenceDetected(writer, this.objectType);
			if (writer.Context.Hierarchy.Count >= writer.Context.MaxHierarchyDepth)
				throw JsonSerializationException.SerializationGraphIsTooDeep(writer, (ulong)writer.Context.MaxHierarchyDepth);

			writer.Context.Hierarchy.Push(value);
			try
			{
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
			finally
			{
				writer.Context.Hierarchy.Pop();
			}
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
						throw JsonSerializationException.FailedToResolveMemberType(reader, typeName, memberName, this.objectType, getTypeError);
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
					throw JsonSerializationException.FailedToReadMemberValue(reader, memberName, this.objectType, e);
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
					throw JsonSerializationException.FailedToSetMemberValue(memberName, value, e);
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

		/// <summary>
		/// Creates an instance of an object from the specified values.
		/// </summary>
		/// <param name="values">The values to populate the instance with.</param>
		/// <returns>The created instance.</returns>
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

		/// <summary>
		/// Creates an instance of the specified type from the specified values.
		/// </summary>
		/// <param name="values">The values to populate the instance with.</param>
		/// <param name="instanceType">The type of the instance to create.</param>
		/// <returns>The created instance.</returns>
		public static object CreateInstance(IndexedDictionary<string, object> values, Type instanceType)
		{
			if (instanceType == null) throw new ArgumentNullException("instanceType");
			if (values == null) throw new ArgumentNullException("values");

			var context = new SerializationContext();
			var serializer = new ObjectSerializer(context, instanceType);
			return serializer.PopulateInstance(values, null);
		}

		/// <summary>
		/// Gets the version-invariant name of the specified type.
		/// </summary>
		/// <param name="type">The type to get the name for.</param>
		/// <returns>The version-invariant name.</returns>
		public static string GetVersionInvariantObjectTypeName(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			var fullName = (type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
			fullName = VersionRegEx.Replace(fullName, string.Empty);
			fullName = fullName.Replace(BclTypePart, ""); // remove BCL path of type information for better interop compatibility
			return fullName;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("object, {0}", this.objectType);
		}
	}
}

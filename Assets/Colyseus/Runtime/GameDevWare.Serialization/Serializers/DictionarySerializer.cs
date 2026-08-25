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
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	/// <summary>
	/// Serializer for <see cref="IDictionary"/> types.
	/// </summary>
	public sealed class DictionarySerializer : TypeSerializer
	{
		private readonly Type dictionaryType;
		private readonly Type instantiatedDictionaryType;
		private readonly Type keyType;
		private readonly Type valueType;
		private readonly bool isStringKeyType;

		/// <inheritdoc />
		public override Type SerializedType { get { return this.dictionaryType; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="DictionarySerializer"/> class.
		/// </summary>
		/// <param name="dictionaryType">The type of the dictionary to serialize or deserialize.</param>
		public DictionarySerializer(Type dictionaryType)
		{
			if (dictionaryType == null)
				throw new ArgumentNullException("dictionaryType");

			this.dictionaryType =
			this.instantiatedDictionaryType = dictionaryType;
			this.keyType = typeof(object);
			this.valueType = typeof(object);

			if (dictionaryType.HasMultipleInstantiations(typeof(IDictionary<,>)))
				throw JsonSerializationException.TypeIsNotValid(this.GetType(), "have only one generic IDictionary<,> interface");


			if (dictionaryType.IsInstantiationOf(typeof(IDictionary<,>)))
			{
				var genArgs = dictionaryType.GetInstantiationArguments(typeof(IDictionary<,>));
				this.keyType = genArgs[0];
				this.valueType = genArgs[1];

				if (dictionaryType.IsInterface && dictionaryType.IsGenericType && dictionaryType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
					this.instantiatedDictionaryType = typeof(Dictionary<,>).MakeGenericType(genArgs);
				else if (typeof(IDictionary).IsAssignableFrom(dictionaryType) == false)
					throw JsonSerializationException.TypeIsNotValid(this.GetType(), "should implement IDictionary interface");
			}
			else if (typeof(IDictionary).IsAssignableFrom(dictionaryType))
			{
				if (dictionaryType == typeof(IDictionary))
				{
					this.instantiatedDictionaryType = typeof(IndexedDictionary<string, object>);
					this.keyType = typeof(string);
					this.valueType = typeof(object);
				}
			}
			else
			{
				throw JsonSerializationException.TypeIsNotValid(this.GetType(), "should implement IDictionary interface");
			}

			this.isStringKeyType = this.keyType == typeof(string);
		}

		/// <inheritdoc />
		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			if (reader.Context.Hierarchy.Count >= reader.Context.MaxHierarchyDepth)
				throw JsonSerializationException.SerializationGraphIsTooDeep(reader, (ulong)reader.Context.MaxHierarchyDepth);

			var container = new List<DictionaryEntry>();
			reader.Context.Hierarchy.Push(container);
			try
			{
				if (reader.Token == JsonToken.BeginArray)
				{
					reader.ReadArrayBegin();
					while (reader.Token != JsonToken.EndOfArray)
					{
						var entry = default(DictionaryEntry);

						if (reader.Token == JsonToken.BeginArray)
						{
							reader.ReadArrayBegin();
							try { entry.Key = reader.ReadValue(this.keyType); }
							catch (Exception e) { throw JsonSerializationException.FailedToReadDictionaryKey(reader, this.keyType, e); }
							reader.Context.Path.Push(new PathSegment(entry.Key));
							try { entry.Value = reader.ReadValue(this.valueType); }
							catch (Exception e) { throw JsonSerializationException.FailedToReadDictionaryValue(reader, this.valueType, entry.Key, e); }
							reader.Context.Path.Pop();
							reader.ReadArrayEnd();
						}
						else
						{
							reader.ReadObjectBegin();
							var keyIsPushed = false;
							while (reader.Token != JsonToken.EndOfObject)
							{
								var memberName = reader.ReadMember();
								switch (memberName)
								{
									case DictionaryEntrySerializer.KEY_MEMBER_NAME:
										try { entry.Key = reader.ReadValue(this.keyType); }
										catch (Exception e) { throw JsonSerializationException.FailedToReadDictionaryKey(reader, this.keyType, e); }
										if (!keyIsPushed)
										{
											reader.Context.Path.Push(new PathSegment(entry.Key));
											keyIsPushed = true;
										}
										break;
									case DictionaryEntrySerializer.VALUE_MEMBER_NAME:
										try { entry.Value = reader.ReadValue(this.valueType); }
										catch (Exception e) { throw JsonSerializationException.FailedToReadDictionaryValue(reader, this.valueType, entry.Key ?? "<unknown>", e); }
										break;
									case ObjectSerializer.TYPE_MEMBER_NAME:
										reader.ReadValue(typeof(object));
										break;
									default:
										throw JsonSerializationException.UnexpectedMemberName(memberName, string.Format("'{0}' or '{1}'", DictionaryEntrySerializer.KEY_MEMBER_NAME, DictionaryEntrySerializer.VALUE_MEMBER_NAME), reader);
								}

							}
							if (keyIsPushed)
							{
								reader.Context.Path.Pop();
							}
							reader.ReadObjectEnd();
						}
						container.Add(entry);
					}
					reader.ReadArrayEnd(nextToken: false);
				}
				else if (reader.Token == JsonToken.BeginObject)
				{
					reader.ReadObjectBegin();
					while (reader.Token != JsonToken.EndOfObject)
					{
						var entry = default(DictionaryEntry);

						try { entry.Key = reader.ReadValue(this.keyType); }
						catch (Exception e) { throw JsonSerializationException.FailedToReadDictionaryKey(reader, this.keyType, e); }
						reader.Context.Path.Push(new PathSegment(entry.Key));

						try { entry.Value = reader.ReadValue(this.valueType); }
						catch (Exception e) { throw JsonSerializationException.FailedToReadDictionaryValue(reader, this.valueType, entry.Key, e); }

						reader.Context.Path.Pop();
						container.Add(entry);
					}
					reader.ReadObjectEnd(nextToken: false);
				}
				else
				{
					throw JsonSerializationException.UnexpectedToken(reader, JsonToken.BeginObject, JsonToken.BeginArray);
				}

				var dictionary = (IDictionary)Activator.CreateInstance(this.instantiatedDictionaryType);
				foreach (var kv in container)
				{
					var key = kv.Key;
					var value = kv.Value;

					if (key.GetType() != this.keyType && this.keyType != typeof(object))
					{
						if (this.keyType.IsEnum)
							key = Enum.Parse(this.keyType, Convert.ToString(key));
						else
							key = Convert.ChangeType(key, this.keyType);
					}

					if (dictionary.Contains(key))
						dictionary.Remove(key);

					dictionary.Add(key, value);
				}

				return dictionary;
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
				throw JsonSerializationException.CircularReferenceDetected(writer, this.dictionaryType);
			if (writer.Context.Hierarchy.Count >= writer.Context.MaxHierarchyDepth)
				throw JsonSerializationException.SerializationGraphIsTooDeep(writer, (ulong)writer.Context.MaxHierarchyDepth);

			var dictionary = (IDictionary)value;
			// ReSharper disable PossibleMultipleEnumeration
			writer.Context.Hierarchy.Push(value);
			try
			{
				// object
				if (this.isStringKeyType)
				{
					writer.WriteObjectBegin(dictionary.Count);
					foreach (DictionaryEntry pair in dictionary)
					{
						var keyStr = Convert.ToString(pair.Key, writer.Context.Format);
						writer.Context.Path.Push(new PathSegment(keyStr));

						// key
						writer.WriteMember(keyStr);
						// value
						writer.WriteValue(pair.Value, this.valueType);

						writer.Context.Path.Pop();
					}
					writer.WriteObjectEnd();
				}
				else
				{
					writer.WriteArrayBegin(dictionary.Count);
					foreach (DictionaryEntry pair in dictionary)
					{
						writer.Context.Path.Push(new PathSegment(pair.Key));
						writer.WriteArrayBegin(2);
						writer.WriteValue(pair.Key, this.keyType);
						writer.WriteValue(pair.Value, this.valueType);
						writer.WriteArrayEnd();
						writer.Context.Path.Pop();
					}
					writer.WriteArrayEnd();
				}
			}
			finally
			{
				writer.Context.Hierarchy.Pop();
			}
			// ReSharper restore PossibleMultipleEnumeration
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("dictionary of {1}:{2}, {0}", this.dictionaryType, this.keyType, this.valueType);
		}
	}
}

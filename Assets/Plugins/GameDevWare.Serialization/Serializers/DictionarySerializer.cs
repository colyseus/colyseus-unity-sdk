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
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public sealed class DictionarySerializer : TypeSerializer
	{
		private readonly Type dictionaryType;
		private readonly Type instantiatedDictionaryType;
		private readonly Type keyType;
		private readonly Type valueType;
		private readonly bool isStringKeyType;

		public override Type SerializedType { get { return this.dictionaryType; } }

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

		public override object Deserialize(IJsonReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

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
							catch (Exception e) { throw new SerializationException(string.Format("Failed to read '{0}' key of dictionary: {1}\r\nMore detailed information in inner exception.", this.keyType.Name, e.Message), e); }
							try { entry.Value = reader.ReadValue(this.valueType); }
							catch (Exception e) { throw new SerializationException(string.Format("Failed to read '{0}' value for key '{1}' in dictionary: {2}\r\nMore detailed information in inner exception.", this.valueType.Name, entry.Key, e.Message), e); }
							reader.ReadArrayEnd();
						}
						else
						{
							reader.ReadObjectBegin();
							while (reader.Token != JsonToken.EndOfObject)
							{
								var memberName = reader.ReadMember();
								switch (memberName)
								{
									case DictionaryEntrySerializer.KEY_MEMBER_NAME:
										try { entry.Key = reader.ReadValue(this.keyType); }
										catch (Exception e) { throw new SerializationException(string.Format("Failed to read '{0}' key of dictionary: {1}\r\nMore detailed information in inner exception.", this.keyType.Name, e.Message), e); }
										break;
									case DictionaryEntrySerializer.VALUE_MEMBER_NAME:
										try { entry.Value = reader.ReadValue(this.valueType); }
										catch (Exception e) { throw new SerializationException(string.Format("Failed to read '{0}' value for key '{1}' in dictionary: {2}\r\nMore detailed information in inner exception.", this.valueType.Name, entry.Key ?? "<unknown>", e.Message), e); }
										break;
									case ObjectSerializer.TYPE_MEMBER_NAME:
										reader.ReadValue(typeof(object));
										break;
									default:
										throw new SerializationException(string.Format("Unknown member found '{0}' in dictionary entry while '{1}' or '{2}' are expected.", memberName, DictionaryEntrySerializer.KEY_MEMBER_NAME, DictionaryEntrySerializer.VALUE_MEMBER_NAME));
								}
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
						catch (Exception e) { throw new SerializationException(string.Format("Failed to read '{0}' key of dictionary: {1}\r\nMore detailed information in inner exception.", this.keyType.Name, e.Message), e); }

						try { entry.Value = reader.ReadValue(this.valueType); }
						catch (Exception e) { throw new SerializationException(string.Format("Failed to read '{0}' value for key '{1}' in dictionary: {2}\r\nMore detailed information in inner exception.", this.valueType.Name, entry.Key, e.Message), e); }

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
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (value == null) throw new ArgumentNullException("value");

			var dictionary = (IDictionary)value;
			// ReSharper disable PossibleMultipleEnumeration
			writer.Context.Hierarchy.Push(value);
			// object
			if (this.isStringKeyType)
			{
				writer.WriteObjectBegin(dictionary.Count);
				foreach (DictionaryEntry pair in dictionary)
				{
					var keyStr = Convert.ToString(pair.Key, writer.Context.Format);
					// key
					writer.WriteMember(keyStr);
					// value
					writer.WriteValue(pair.Value, this.valueType);
				}
				writer.WriteObjectEnd();
			}
			else
			{
				writer.WriteArrayBegin(dictionary.Count);
				foreach (DictionaryEntry pair in dictionary)
				{
					writer.WriteArrayBegin(2);
					writer.WriteValue(pair.Key, this.keyType);
					writer.WriteValue(pair.Value, this.valueType);
					writer.WriteArrayEnd();
				}
				writer.WriteArrayEnd();
			}
			// ReSharper restore PossibleMultipleEnumeration

			writer.Context.Hierarchy.Pop();
		}

		public override string ToString()
		{
			return string.Format("dictionary of {1}:{2}, {0}", this.dictionaryType, this.keyType, this.valueType);
		}
	}
}

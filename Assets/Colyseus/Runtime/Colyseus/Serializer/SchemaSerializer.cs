using System;
using System.Reflection;
using System.Collections.Generic;
using Colyseus.Schema;
using Type = Colyseus.Schema.Type;

namespace Colyseus
{
	/// <summary>
	///     An instance of ISerializer specifically for <see cref="Schema" /> based serialization
	/// </summary>
	/// <typeparam name="T">A child of <see cref="Schema" /></typeparam>
	public class ColyseusSchemaSerializer<T> : IColyseusSerializer<T> where T : Schema.Schema
	{
		public Decoder<T> Decoder = new Decoder<T>();

		/// <summary>
		///     A reference to the <see cref="Iterator" />
		/// </summary>
		protected Iterator It = new Iterator();

		/// <inheritdoc />
		public void SetState(byte[] data, int offset = 0)
		{
			It.Offset = offset;
			Decoder.Decode(data, It);
		}

		/// <inheritdoc />
		public T GetState()
		{
			return Decoder.State;
		}

		/// <inheritdoc />
		public void Patch(byte[] data, int offset = 0)
		{
			It.Offset = offset;
			Decoder.Decode(data, It);
		}

		/// <inheritdoc />
		public void Teardown()
		{
			// Clear all stored references.
			Decoder.Teardown();
		}

		/// <inheritdoc />
		public void Handshake(byte[] bytes, int offset)
		{
			System.Type targetType = typeof(T);

			System.Type[] allTypes = targetType.Assembly.GetTypes();
			List<System.Type> namespaceSchemaTypes = new List<System.Type>(Array.FindAll(allTypes, t => t.Namespace == targetType.Namespace && typeof(Schema.Schema).IsAssignableFrom( targetType)));

			Iterator it = new Iterator { Offset = offset };

			var reflectionDecoder = new Decoder<Reflection>();
			reflectionDecoder.Decode(bytes, it);

			var reflection = reflectionDecoder.State;
			var types = reflection.types.items.ToArray();

			for (int i = 0; i < reflection.types.Count; i++)
			{
				var reflectionType = reflection.types[i];
				var reflectionFields = GetFieldsFromType(reflectionType, types);

				var schemaType = namespaceSchemaTypes.Find(t => CompareTypes(t, reflectionFields));

				if (schemaType != null)
				{
					// UnityEngine.Debug.Log("FOUND: " + schemaType.FullName + " => " + DebugReflectionType(reflectionType, reflectionFields));

					Decoder.Context.SetTypeId(schemaType, reflection.types[i].id);

					// Remove from list to avoid duplicate checks
					namespaceSchemaTypes.Remove(schemaType);

				}
				else
				{
					// UnityEngine.Debug.Log("NOT FOUND: " + DebugReflectionType(reflectionType, reflectionFields));

					UnityEngine.Debug.LogWarning(
						"Local schema mismatch from server. Use \"schema-codegen\" to generate up-to-date local definitions.");
				}
			}
		}

		private static string DebugReflectionType(ReflectionType reflectionType, List<ReflectionField> reflectionFields)
		{
			List<string> fieldNames = new List<string>();
			for (int i = 0; i < reflectionFields.Count; i++)
			{
				fieldNames.Add(reflectionFields[i].name);
			}
			return $"TypeId: {reflectionType.id} (extendsId: {reflectionType.extendsId}), Fields: {string.Join(", ", fieldNames)}";
		}

		private static bool CompareTypes(System.Type schemaType, List<ReflectionField> reflectionFields)
		{
			FieldInfo[] fields = schemaType.GetFields();
			int typedFieldCount = 0;

			foreach (FieldInfo field in fields)
			{
				object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);
				if (typeAttributes.Length != 1)
				{
					continue;
				}

				Type typedField = (Type)typeAttributes[0];

				// Skip if reflectionType doesn't have the field
				if (typedField.Index >= reflectionFields.Count)
				{
					return false;
				}

				ReflectionField reflectionField = reflectionFields[typedField.Index];

				if (
					reflectionField.type.IndexOf(typedField.FieldType) != 0 ||
					reflectionField.name != field.Name
				)
				{
					return false;
				}

				typedFieldCount++;
			}

			// skip if number of Type'd fields doesn't match
			if (typedFieldCount != reflectionFields.Count)
			{
				return false;
			}

			return true;
		}

		private List<ReflectionField> GetFieldsFromType(ReflectionType reflectionType, ReflectionType[] types)
		{
			var reflectionFields = new List<ReflectionField>();

			// Find all types in the inheritance chain from child to root
			List<ReflectionType> inheritanceChain = new List<ReflectionType>();
			var extendsId = reflectionType.id;
			while (extendsId != -1)
			{
				var currentType = Array.Find(types, t => t.id == extendsId);
				inheritanceChain.Insert(0, currentType); // Insert at the beginning to reverse order
				extendsId = currentType.extendsId;
			}

			// Collect fields from each type in the chain, from root to child
			foreach (var type in inheritanceChain)
			{
				type.fields.ForEach((_, field) => reflectionFields.Add(field));
			}

			return reflectionFields;
		}
	}
}
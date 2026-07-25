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
	public class SchemaSerializer<T> : ISerializer<T> where T : Schema.Schema
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
			if (Decoder.Refs.refs.Count > 1)
			{
				// rejoin over live state: reconcile ghosts (deletions that
				// happened while off the wire) instead of decoding additively
				Decoder.DecodeResync(data, It);
			}
			else
			{
				Decoder.Decode(data, It);
			}
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
			Iterator it = new Iterator { Offset = offset };

			var reflectionDecoder = new Decoder<Reflection>();
			reflectionDecoder.Decode(bytes, it);

			var reflection = reflectionDecoder.State;
			var types = reflection.types.items.ToArray();

			if (typeof(T) == typeof(DynamicSchema))
			{
				HandshakeDynamic(reflection, types);
				return;
			}

			System.Type targetType = typeof(T);

			System.Type[] allTypes = targetType.Assembly.GetTypes();
			List<System.Type> namespaceSchemaTypes = new List<System.Type>(Array.FindAll(allTypes, t => t.Namespace == targetType.Namespace && typeof(Schema.Schema).IsAssignableFrom( targetType)));

			for (int i = 0; i < reflection.types.Count; i++)
			{
				var reflectionType = reflection.types[i];
				var reflectionFields = GetFieldsFromType(reflectionType, types);

				var schemaType = namespaceSchemaTypes.Find(t => CompareTypes(t, reflectionFields));

				if (schemaType != null)
				{
					Decoder.Context.SetTypeId(schemaType, reflection.types[i].id);

					// Remove from list to avoid duplicate checks
					namespaceSchemaTypes.Remove(schemaType);

				}
				else
				{
					ColyseusContext.Logger.LogWarning(
						"Local schema mismatch from server. Use \"schema-codegen\" to generate up-to-date local definitions.");
				}
			}
		}

		private void HandshakeDynamic(Reflection reflection, ReflectionType[] types)
		{
			Decoder.DynamicDefinitions = new Dictionary<float, DynamicTypeDefinition>();

			for (int i = 0; i < reflection.types.Count; i++)
			{
				var reflectionType = reflection.types[i];
				var reflectionFields = GetFieldsFromType(reflectionType, types);

				var definition = new DynamicTypeDefinition();
				definition.TypeId = reflectionType.id;

				for (int j = 0; j < reflectionFields.Count; j++)
				{
					definition.ParseFieldType(reflectionFields[j], j);
				}

				Decoder.DynamicDefinitions[reflectionType.id] = definition;
				Decoder.Context.SetTypeId(typeof(DynamicSchema), reflectionType.id);
			}

			// Assign root definition
			var rootTypeId = reflection.rootType >= 0
				? reflection.rootType
				: (reflection.types.Count > 0 ? reflection.types[0].id : -1);

			if (rootTypeId >= 0)
			{
				var rootState = Decoder.State as DynamicSchema;
				if (rootState != null && Decoder.DynamicDefinitions.TryGetValue(rootTypeId, out var rootDef))
				{
					rootState.Definition = rootDef;
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

		/// <summary>
		///     Decode reflection bytes and build the ROOT type's
		///     <see cref="DynamicTypeDefinition" /> — used by the Room to
		///     synthesize the input schema from the INPUT_REFLECTION handshake
		///     section.
		/// </summary>
		internal static DynamicTypeDefinition BuildDynamicDefinition(byte[] bytes, int offset)
		{
			var it = new Iterator { Offset = offset };
			var reflectionDecoder = new Decoder<Reflection>();
			reflectionDecoder.Decode(bytes, it);

			var reflection = reflectionDecoder.State;
			var types = reflection.types.items.ToArray();

			var rootTypeId = reflection.rootType >= 0
				? reflection.rootType
				: (reflection.types.Count > 0 ? reflection.types[0].id : -1);
			var rootType = Array.Find(types, t => t.id == rootTypeId);
			if (rootType == null)
			{
				throw new Exception("BuildDynamicDefinition: reflection has no root type");
			}

			var fields = GetFieldsFromType(rootType, types);
			var definition = new DynamicTypeDefinition { TypeId = rootType.id };
			for (int j = 0; j < fields.Count; j++)
			{
				definition.ParseFieldType(fields[j], j);
			}
			return definition;
		}

		private static List<ReflectionField> GetFieldsFromType(ReflectionType reflectionType, ReflectionType[] types)
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
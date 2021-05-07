using System;
using System.Reflection;
using Colyseus.Schema;
using Type = Colyseus.Schema.Type;

namespace Colyseus
{
    /// <summary>
    ///     An instance of ISerializer specifically for <see cref="Schema" /> based serialization
    /// </summary>
    /// <typeparam name="T">A child of <see cref="Schema" /></typeparam>
    public class ColyseusSchemaSerializer<T> : IColyseusSerializer<T>
    {
        /// <summary>
        ///     A reference to the <see cref="Iterator" />
        /// </summary>
        protected Iterator it = new Iterator();

        /// <summary>
        ///     Used for tracking all references
        /// </summary>
        protected ColyseusReferenceTracker refs = new ColyseusReferenceTracker();

        /// <summary>
        ///     The current state of this Serializer
        /// </summary>
        protected T state;

        public ColyseusSchemaSerializer()
        {
            state = Activator.CreateInstance<T>();
        }

        /// <inheritdoc />
        public void SetState(byte[] data, int offset = 0)
        {
            it.Offset = offset;
            (state as Schema.Schema)?.Decode(data, it, refs);
        }

        /// <inheritdoc />
        public T GetState()
        {
            return state;
        }

        /// <inheritdoc />
        public void Patch(byte[] data, int offset = 0)
        {
            it.Offset = offset;
            (state as Schema.Schema)?.Decode(data, it, refs);
        }

        /// <inheritdoc />
        public void Teardown()
        {
            // Clear all stored references.
            refs.Clear();
        }

        /// <inheritdoc />
        public void Handshake(byte[] bytes, int offset)
        {
            System.Type targetType = typeof(T);

            System.Type[] allTypes = targetType.Assembly.GetTypes();
            System.Type[] namespaceSchemaTypes = Array.FindAll(allTypes, t => t.Namespace == targetType.Namespace &&
                                                                              typeof(Schema.Schema).IsAssignableFrom(
                                                                                  targetType));

            ColyseusReflection reflection = new ColyseusReflection();
            Iterator it = new Iterator {Offset = offset};

            reflection.Decode(bytes, it);

            for (int i = 0; i < reflection.types.Count; i++)
            {
                System.Type schemaType = Array.Find(namespaceSchemaTypes, t => CompareTypes(t, reflection.types[i]));

                if (schemaType == null)
                {
                    throw new Exception(
                        "Local schema mismatch from server. Use \"schema-codegen\" to generate up-to-date local definitions.");
                }

                ColyseusContext.GetInstance().SetTypeId(schemaType, reflection.types[i].id);
            }
        }

        private static bool CompareTypes(System.Type schemaType, ReflectionType reflectionType)
        {
            FieldInfo[] fields = schemaType.GetFields();
            int typedFieldCount = 0;

            string fieldNames = "";
            for (int i = 0; i < fields.Length; i++)
            {
                fieldNames += fields[i].Name + ", ";
            }

            foreach (FieldInfo field in fields)
            {
                object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);

                if (typeAttributes.Length == 1)
                {
                    Type typedField = (Type) typeAttributes[0];
                    ReflectionField reflectionField = reflectionType.fields[typedField.Index];

                    if (
                        reflectionField == null ||
                        reflectionField.type.IndexOf(typedField.FieldType) != 0 ||
                        reflectionField.name != field.Name
                    )
                    {
                        return false;
                    }

                    typedFieldCount++;
                }
            }

            // skip if number of Type'd fields doesn't match
            if (typedFieldCount != reflectionType.fields.Count)
            {
                return false;
            }

            return true;
        }
    }
}
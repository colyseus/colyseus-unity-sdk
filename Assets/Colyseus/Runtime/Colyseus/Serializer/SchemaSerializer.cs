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
    public class ColyseusSchemaSerializer<T> : IColyseusSerializer<T> where T: Schema.Schema
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
            System.Type[] namespaceSchemaTypes = Array.FindAll(allTypes, t => t.Namespace == targetType.Namespace &&
                                                                              typeof(Schema.Schema).IsAssignableFrom(
                                                                                  targetType));

            Iterator it = new Iterator { Offset = offset };

            var reflectionDecoder = new Decoder<ColyseusReflection>();
            reflectionDecoder.Decode(bytes, it);

            var reflection = reflectionDecoder.State;

            for (int i = 0; i < reflection.types.Count; i++)
            {
                System.Type schemaType = Array.Find(namespaceSchemaTypes, t => CompareTypes(t, reflection.types[i]));

                if (schemaType != null)
                {
                    Decoder.Context.SetTypeId(schemaType, reflection.types[i].id);   
                } 
                else 
                {
                    UnityEngine.Debug.LogWarning(
                        "Local schema mismatch from server. Use \"schema-codegen\" to generate up-to-date local definitions.");
                }
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
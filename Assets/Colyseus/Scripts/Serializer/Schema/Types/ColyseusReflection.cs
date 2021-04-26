namespace Colyseus.Schema
{
    /// <summary>
    ///     <see cref="Schema" /> used for the purposes of reflection
    /// </summary>
    public class ReflectionField : Schema
    {
        /// <summary>
        ///     The field name
        /// </summary>
        [Type(0, "string")]
        public string name;

        /// <summary>
        ///     The type of the field
        /// </summary>
        [Type(1, "string")]
        public string type;

        [Type(2, "number")]
        public float referencedType;
    }

    /// <summary>
    ///     Mid level reflection container of an <see cref="ArraySchema{T}" />
    /// </summary>
    public class ReflectionType : Schema
    {
        /// <summary>
        ///     The ID of this <see cref="Schema" />
        /// </summary>
        [Type(0, "number")]
        public float id;

        /// <summary>
        ///     An <see cref="ArraySchema{T}" /> of <see cref="ReflectionField" />
        /// </summary>
        [Type(1, "array", typeof(ArraySchema<ReflectionField>))]
        public ArraySchema<ReflectionField> fields = new ArraySchema<ReflectionField>();
    }

    /// <summary>
    ///     Top level reflection container for an <see cref="ArraySchema{T}" />
    /// </summary>
    public class ColyseusReflection : Schema
    {
        /// <summary>
        ///     An <see cref="ArraySchema{T}" /> of <see cref="ReflectionType" />
        /// </summary>
        [Type(0, "array", typeof(ArraySchema<ReflectionType>))]
        public ArraySchema<ReflectionType> types = new ArraySchema<ReflectionType>();

        [Type(1, "number")]
        public float rootType;
    }
}
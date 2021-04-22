namespace Colyseus.Schema
{
    /// <summary>
    ///     <see cref="Schema" /> used for the purposes of reflection
    /// </summary>
    public class CSAReflectionField : Schema
    {
        /// <summary>
        ///     The field name
        /// </summary>
        [Type(0, "string")]
        public string name;

        [Type(2, "number")]
        public float referencedType; //TODO: remove unused attribute?

        /// <summary>
        ///     The type of the field
        /// </summary>
        [Type(1, "string")]
        public string type;
    }

    /// <summary>
    ///     Mid level reflection container of an <see cref="ArraySchema{T}" />
    /// </summary>
    public class CSAReflectionType : Schema
    {
        /// <summary>
        ///     An <see cref="ArraySchema{T}" /> of <see cref="CSAReflectionField" />
        /// </summary>
        [Type(1, "array", typeof(ArraySchema<CSAReflectionField>))]
        public ArraySchema<CSAReflectionField> fields = new ArraySchema<CSAReflectionField>();

        /// <summary>
        ///     The ID of this <see cref="Schema" />
        /// </summary>
        [Type(0, "number")]
        public float id;
    }

    /// <summary>
    ///     Top level reflection container for an <see cref="ArraySchema{T}" />
    /// </summary>
    public class ColyseusReflection : Schema
    {
        [Type(1, "number")]
        public float rootType;  //TODO: remove unused attribute?

        /// <summary>
        ///     An <see cref="ArraySchema{T}" /> of <see cref="CSAReflectionType" />
        /// </summary>
        [Type(0, "array", typeof(ArraySchema<CSAReflectionType>))]
        public ArraySchema<CSAReflectionType> types = new ArraySchema<CSAReflectionType>();
    }
}
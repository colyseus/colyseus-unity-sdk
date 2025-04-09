using UnityEngine.Scripting;

namespace Colyseus.Schema
{
    /// <summary>
    ///     <see cref="Schema" /> used for the purposes of reflection
    /// </summary>
    [Preserve]
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
        public float referencedType = -1;
    }

    /// <summary>
    ///     Mid level reflection container of an <see cref="ArraySchema{T}" />
    /// </summary>
    [Preserve]
    public class ReflectionType : Schema
    {
        /// <summary>
        ///     The ID of this <see cref="Schema" />
        /// </summary>
        [Type(0, "number")]
        public float id;

        /// <summary>
        ///     The ID of which structure this <see cref="Schema" /> extends
        /// </summary>
        [Type(1, "number")]
        public float extendsId = -1;

        /// <summary>
        ///     An <see cref="ArraySchema{T}" /> of <see cref="ReflectionField" />
        /// </summary>
        [Preserve]
        [Type(2, "array", typeof(ArraySchema<ReflectionField>))]
        public ArraySchema<ReflectionField> fields;

        [Preserve]
        public ReflectionType() {}
    }

    /// <summary>
    ///     Top level reflection container for an <see cref="ArraySchema{T}" />
    /// </summary>
    [Preserve]
    public class Reflection : Schema
    {
        /// <summary>
        ///     An <see cref="ArraySchema{T}" /> of <see cref="ReflectionType" />
        /// </summary>
        [Preserve]
        [Type(0, "array", typeof(ArraySchema<ReflectionType>))]
        public ArraySchema<ReflectionType> types;

        [Type(1, "number")]
        public float rootType = -1;
    }
}

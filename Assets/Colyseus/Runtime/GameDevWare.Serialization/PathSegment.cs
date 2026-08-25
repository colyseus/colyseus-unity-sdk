using System;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Represents a segment in a serialization path.
	/// </summary>
	public struct PathSegment
	{
		/// <summary>
		/// Gets the index of the segment, if it represents an array element.
		/// </summary>
		public readonly int Index;
		/// <summary>
		/// Gets the member name of the segment, if it represents an object member.
		/// </summary>
		public readonly object MemberName;

		/// <summary>
		/// Initializes a new instance of the <see cref="PathSegment"/> struct with an index.
		/// </summary>
		/// <param name="index">The index of the array element.</param>
		public PathSegment(int index)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index");

			this.Index = index;
			this.MemberName = null;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="PathSegment"/> struct with a member name.
		/// </summary>
		/// <param name="memberName">The name of the object member.</param>
		public PathSegment(object memberName)
		{
			if (memberName == null) throw new ArgumentNullException("memberName");

			this.Index = -1;
			this.MemberName = memberName;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			if (this.Index >= 0)
			{
				return this.Index.ToString();
			}
			else if (this.MemberName != null)
			{
				return this.MemberName.ToString();
			}
			else
			{
				return string.Empty;
			}
		}
	}
}

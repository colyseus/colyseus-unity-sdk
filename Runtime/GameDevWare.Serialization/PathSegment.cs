using System;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	public struct PathSegment
	{
		public readonly int Index;
		public readonly object MemberName;

		public PathSegment(int index)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index");

			this.Index = index;
			this.MemberName = null;
		}
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

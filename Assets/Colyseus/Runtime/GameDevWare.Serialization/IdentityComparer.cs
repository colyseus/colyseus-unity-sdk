using System;
using System.Collections.Generic;

internal sealed class IdentityComparer : IEqualityComparer<object>
{
	public static readonly IdentityComparer Default = new IdentityComparer();

	public new bool Equals(object x, object y)
	{
		return ReferenceEquals(x, y);
	}
	public int GetHashCode(object obj)
	{
		return obj == null ? 0 : obj.GetHashCode();
	}
}

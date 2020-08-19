using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	public class ReferenceTracker
	{
		public Dictionary<int, IRef> refs = new Dictionary<int, IRef>();
		public Dictionary<int, int> refCounts = new Dictionary<int, int>();
		public List<int> deletedRefs = new List<int>();

		public ReferenceTracker() {}

		public void Add(int refId, IRef _ref, bool incrementCount = true)
		{
			int previousCount;

			if (!refs.ContainsKey(refId))
			{
				refs[refId] = _ref;
				previousCount = 0;
			} else
			{
				previousCount = refCounts[refId];
			}

			if (incrementCount)
			{
				refCounts[refId] = previousCount + 1;
			}
		}

		public IRef Get(int refId)
		{
			IRef _ref = null;

			refs.TryGetValue(refId, out _ref);

			return _ref;
		}

		public bool Has(int refId)
		{
			return refs.ContainsKey(refId);
		}

		public bool Remove(int refId)
		{
			refCounts[refId] = refCounts[refId] - 1;

			if (!deletedRefs.Contains(refId))
			{
				deletedRefs.Add(refId);
				return true;
			} else
			{
				return false;
			}
		}

		public void GarbageCollection()
		{
			int totalDeletedRefs = deletedRefs.Count;
			for (int i = 0; i < totalDeletedRefs; i++)
			{
				int refId = deletedRefs[i];

				if (refCounts[refId] <= 0)
				{
					var _ref = refs[refId];
					if (_ref is Schema)
					{
						foreach (KeyValuePair<string, System.Type> field in ((Schema)_ref).GetFieldChildTypes())
						{
							var fieldValue = ((Schema)_ref)[field.Key];
							if (
								fieldValue is IRef &&
								Remove(((IRef)fieldValue).__refId))
							{
								totalDeletedRefs++;
							}
						}
					}
					else if (_ref is ISchemaCollection && ((ISchemaCollection)_ref).HasSchemaChild)
					{
						IDictionary items = ((ISchemaCollection)_ref).GetItems();
						foreach (IRef item in items.Values)
						{
							if (Remove(item.__refId))
							{
								totalDeletedRefs++;
							}
						}
					}
					refs.Remove(refId);
					refCounts.Remove(refId);
				}
			}

			deletedRefs.Clear();
		}

		public void Clear()
		{
			refs.Clear();
			refCounts.Clear();
			deletedRefs.Clear();
		}
	}
}

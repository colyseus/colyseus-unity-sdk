using System;
using UnityEngine;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	public class ReferenceTracker
	{
		protected Dictionary<int, IRef> refs = new Dictionary<int, IRef>();
		protected Dictionary<int, int> refCounts = new Dictionary<int, int>();
		protected List<int> deletedRefs = new List<int>();

		public ReferenceTracker() {}

		public void Add(int refId, IRef _ref)
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

			refCounts[refId] = previousCount + 1;
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

		public void Remove(int refId)
		{
			refCounts[refId] = refCounts[refId] - 1;

			if (!deletedRefs.Contains(refId))
			{
				deletedRefs.Add(refId);
			}
		}

		public void GarbageCollection()
		{
			foreach (int refId in deletedRefs)
			{
				Debug.Log("Deleted ref => " + refId);
				if (refCounts[refId] <= 0)
				{
					var _ref = refs[refId];
					if (_ref is Schema)
					{
						foreach (KeyValuePair<string, System.Type> field in ((Schema)_ref).GetFieldChildTypes())
						{
							var fieldValue = ((Schema)_ref)[field.Key];
							if (fieldValue is IRef)
							{
								Remove(((IRef)fieldValue).__refId);
							}
						}
					} else if (_ref is ISchemaCollection && ((ISchemaCollection)_ref).HasSchemaChild)
					{
						foreach (KeyValuePair<string, IRef> item in ((ISchemaCollection)_ref).GetItems())
						{
							Remove(item.Value.__refId);
						}
					}
					refs.Remove(refId);
					refCounts.Remove(refId);
				}
			}
		}

		public void Clear()
		{
			refs.Clear();
			refCounts.Clear();
			deletedRefs.Clear();
		}
	}
}

using System;
using UnityEngine;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	public class ReferenceTracker
	{
		protected Dictionary<int, IRef> refs = new Dictionary<int, IRef>();

		public ReferenceTracker()
		{
		}

		public void Add(int refId, IRef _ref)
		{
			//Debug.Log("Add ref id => " + refId);

			if (!refs.ContainsKey(refId))
			{
				refs.Add(refId, _ref);
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

		public void Remove(int refId)
		{
			refs.Remove(refId);
		}

		public void GarbageCollection()
		{
	//		this.deletedRefs.forEach((refId) => {
	//		if (this.refCounts[refId] <= 0)
	//		{
	//			const ref = this.refs.get(refId);

	//			//
	//			// Ensure child schema instances have their references removed as well.
	//			//
	//			if (ref instanceof Schema) {
	//			for (const fieldName in ref['_definition'].schema) {
	//				if (
	//					typeof(ref['_definition'].schema[fieldName]) !== "string" &&
	//				   ref[fieldName] &&
	//				   ref[fieldName]['$changes']
 //                       ) {
	//					this.removeRef(ref[fieldName]['$changes'].refId);
	//				}
	//			}

	//		} else
	//		{
	//			const definition: SchemaDefinition = ref['$changes'].parent._definition;
	//			const type = definition.schema[definition.fieldsByIndex[ref['$changes'].parentIndex]];

	//			if (typeof(Object.values(type)[0]) === "function") {
	//				Array.from(ref.values())
	//					.forEach((child) => this.removeRef(child['$changes'].refId));
	//			}
	//		}

	//		this.refs.delete(refId);
	//		delete this.refCounts[refId];
	//	}
	//});
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace Colyseus.Schema
{
    /// <summary>
    ///     Keep track of and maintain multiple <see cref="IRef" /> objects
    /// </summary>
    public class ColyseusReferenceTracker
    {
        /// <summary>
        ///     Local list of <see cref="IRef" />s we have scheduled for removal in the next
        ///     <see cref="GarbageCollection" />
        /// </summary>
        public List<int> deletedRefs = new List<int>();

        /// <summary>
        ///     Map of how many <see cref="IRef" />s we're currently tracking for each ID
        /// </summary>
        public Dictionary<int, int> refCounts = new Dictionary<int, int>();

        /// <summary>
        ///     Map of <see cref="IRef" />s we're tracking
        /// </summary>
        public Dictionary<int, IRef> refs = new Dictionary<int, IRef>();

        /// <summary>
        ///     Add a new reference to be tracked
        /// </summary>
        /// <param name="refId">The ID of the reference</param>
        /// <param name="_ref">The object we're tracking</param>
        /// <param name="incrementCount">If true, we increment the <see cref="refCounts" /> at this ID</param>
        public void Add(int refId, IRef _ref, bool incrementCount = true)
        {
            int previousCount;

            if (!refs.ContainsKey(refId))
            {
                refs[refId] = _ref;
                previousCount = 0;
            }
            else
            {
                previousCount = refCounts[refId];
            }

            if (incrementCount)
            {
                refCounts[refId] = previousCount + 1;
            }
        }

        /// <summary>
        ///     Get a reference by it's ID
        /// </summary>
        /// <param name="refId">The ID of the reference requested</param>
        /// <returns>The reference with that <paramref name="refId" /> in <see cref="refs" />, if it exists</returns>
        public IRef Get(int refId)
        {
            IRef _ref = null;

            refs.TryGetValue(refId, out _ref);

            return _ref;
        }

        /// <summary>
        ///     Check if <see cref="refs" /> contains <paramref name="refId" />
        /// </summary>
        /// <param name="refId">The ID to check for</param>
        /// <returns>True if <see cref="refs" /> contains <paramref name="refId" />, false otherwise</returns>
        public bool Has(int refId)
        {
            return refs.ContainsKey(refId);
        }

        /// <summary>
        ///     Remove a reference by ID
        /// </summary>
        /// <param name="refId">The ID of the reference to remove</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool Remove(int refId)
        {
            refCounts[refId] = refCounts[refId] - 1;

            if (!deletedRefs.Contains(refId))
            {
                deletedRefs.Add(refId);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Remove all references contained in <see cref="deletedRefs"></see> from the tracking maps (<see cref="refs" /> and
        ///     <see cref="refCounts" />). Clears <see cref="deletedRefs" /> afterwards
        /// </summary>
        public void GarbageCollection()
        {
            int totalDeletedRefs = deletedRefs.Count;
            for (int i = 0; i < totalDeletedRefs; i++)
            {
                int refId = deletedRefs[i];

                if (refCounts[refId] <= 0)
                {
                    IRef _ref = refs[refId];
                    if (_ref is Schema)
                    {
                        foreach (KeyValuePair<string, System.Type> field in ((Schema) _ref).GetFieldChildTypes())
                        {
                            object fieldValue = ((Schema) _ref)[field.Key];
                            if (
                                fieldValue is IRef &&
                                Remove(((IRef) fieldValue).__refId))
                            {
                                totalDeletedRefs++;
                            }
                        }
                    }
                    else if (_ref is ISchemaCollection && ((ISchemaCollection) _ref).HasSchemaChild)
                    {
                        IDictionary items = ((ISchemaCollection) _ref).GetItems();
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

        /// <summary>
        ///     Clear all maps and lists in this tracker
        /// </summary>
        public void Clear()
        {
            refs.Clear();
            refCounts.Clear();
            deletedRefs.Clear();
        }
    }
}
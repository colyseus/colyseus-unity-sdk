using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Colyseus.Schema
{
    /// <summary>
    ///     A <see cref="Schema" /> dictionary of <typeparamref name="T" /> type objects.
    ///     <para>
    ///         Enumerates like a <see cref="Dictionary{TKey,TValue}" /> —
    ///         <c>foreach (var (key, value) in map)</c> — but in INSERTION order,
    ///         the order JS iterates a Map in (replacing a key keeps its place;
    ///         removing and re-adding moves it to the end). <see cref="Keys" /> and
    ///         <see cref="Values" /> are typed views in the same order. Every
    ///         <c>foreach</c> over the map or its views is allocation-free.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type of object in this map</typeparam>
    public class MapSchema<T> : IMapSchema, IReadOnlyDictionary<string, T>
    {
        protected Dictionary<int, string> Indexes = new Dictionary<int, string>();

        private struct Entry
        {
            public string Key;   // null = removed (tombstone)
            public T Value;
        }

        /// <summary>
        ///     Insertion-ordered storage: entries append, removals leave a
        ///     tombstone that later compaction squeezes out. Held by reference
        ///     because the decoder re-wraps a collection (<see cref="Clone" />) on
        ///     every re-send, and every wrapper must see the same entries.
        /// </summary>
        private sealed class Store
        {
            public readonly Dictionary<string, int> Slots = new Dictionary<string, int>();
            public Entry[] Entries = new Entry[4];
            public int End;       // slots in use, tombstones included
            public int Version;   // bumped on add/remove/clear — enumerators check it

            public int Count => Slots.Count;

            public bool TryGet(string key, out T value)
            {
                if (Slots.TryGetValue(key, out int slot))
                {
                    value = Entries[slot].Value;
                    return true;
                }
                value = default;
                return false;
            }

            public void Set(string key, T value)
            {
                if (Slots.TryGetValue(key, out int slot))
                {
                    Entries[slot].Value = value;   // replace keeps its position
                    return;
                }
                if (End == Entries.Length) { Array.Resize(ref Entries, Entries.Length * 2); }
                Entries[End] = new Entry { Key = key, Value = value };
                Slots[key] = End++;
                Version++;
            }

            public bool Remove(string key)
            {
                if (!Slots.TryGetValue(key, out int slot)) { return false; }
                Slots.Remove(key);
                Entries[slot] = default;
                Version++;
                if (Slots.Count == 0) { End = 0; }
                else if (End - Slots.Count > Math.Max(8, Slots.Count)) { Compact(); }
                return true;
            }

            private void Compact()
            {
                int write = 0;
                for (int read = 0; read < End; read++)
                {
                    if (Entries[read].Key == null) { continue; }
                    if (write != read)
                    {
                        Entries[write] = Entries[read];
                        Slots[Entries[write].Key] = write;
                    }
                    write++;
                }
                Array.Clear(Entries, write, End - write);
                End = write;
            }

            public void Clear()
            {
                Slots.Clear();
                Array.Clear(Entries, 0, End);
                End = 0;
                Version++;
            }

            /// <summary>Slot of the index-th live entry, in insertion order (never compacts: live enumerators hold slots).</summary>
            public int SlotAt(int index)
            {
                if ((uint)index >= (uint)Slots.Count) { throw new ArgumentOutOfRangeException(nameof(index)); }
                if (End == Slots.Count) { return index; }
                for (int slot = 0, live = -1; slot < End; slot++)
                {
                    if (Entries[slot].Key != null && ++live == index) { return slot; }
                }
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private readonly Store store;
        private KeyCollection keys;
        private ValueCollection values;
        private LegacyItems legacyItems;

        [Preserve]
        public MapSchema()
        {
            store = new Store();
        }

        /// <summary>Copies <paramref name="items" /> into a new map, in its order.</summary>
        [Obsolete("Use the parameterless constructor and Add(key, value) (or a collection initializer).")]
        public MapSchema(OrderedDictionary items = null) : this()
        {
            if (items == null) { return; }
            foreach (DictionaryEntry entry in items) { store.Set((string)entry.Key, (T)entry.Value); }
        }

        private MapSchema(Store store, Dictionary<int, string> indexes)
        {
            this.store = store;
            Indexes = indexes;
        }

        /// <summary>
        ///     Get/set by <paramref name="key" />. A missing key reads as <c>default</c>
        ///     (use <see cref="TryGetValue" /> to tell the two apart).
        /// </summary>
        public T this[string key]
        {
            get
            {
                TryGetValue(key, out T value);
                return value;
            }
            set { store.Set(key, value); }
        }

        /// <summary>The keys, in insertion order.</summary>
        public KeyCollection Keys => keys ??= new KeyCollection(this);

        /// <summary>The values, in insertion order.</summary>
        public ValueCollection Values => values ??= new ValueCollection(this);

        IEnumerable<string> IReadOnlyDictionary<string, T>.Keys => Keys;
        IEnumerable<T> IReadOnlyDictionary<string, T>.Values => Values;

        /// <summary>
        ///     A non-generic dictionary view over this map, kept for code written
        ///     against the old <c>OrderedDictionary items</c> field. Reads and writes
        ///     go straight to the map.
        /// </summary>
        [Obsolete("Enumerate the map directly (KeyValuePair<string, T>), or use Keys / Values / the indexer.")]
        public IDictionary items => LegacyView;

        private LegacyItems LegacyView => legacyItems ??= new LegacyItems(this);

        public int __refId { get; set; }

        /// <summary>
        ///     Set the <see cref="Indexes" /> value
        /// </summary>
        /// <param name="index">The field index</param>
        /// <param name="dynamicIndex">The new dynamic Index value, cast to <see cref="int" /></param>
        public void SetIndex(int index, object dynamicIndex)
        {
            Indexes[index] = (string) dynamicIndex;
        }

        /// <summary>
        ///     Set an Item by it's <paramref name="dynamicIndex" />
        /// </summary>
        /// <param name="index">
        ///     Sets <see cref="Indexes" /> value at <paramref name="index" /> to <paramref name="dynamicIndex" />
        /// </param>
        /// <param name="dynamicIndex">The key that will be set to <paramref name="value" /></param>
        /// <param name="value">The new object to put into the map</param>
        public void SetByIndex(int index, object dynamicIndex, object value)
        {
            Indexes[index] = (string) dynamicIndex;
            store.Set((string) dynamicIndex, (T) value);
        }

        /// <summary>
        ///     Get the dynamic index value from <see cref="Indexes" />
        /// </summary>
        /// <param name="index">The location of the dynamic index to return</param>
        /// <returns>The dynamic index from <see cref="Indexes" />, if it exists. <c>null</c> if it does not</returns>
        public object GetIndex(int index)
        {
            string dynamicIndex;

            Indexes.TryGetValue(index, out dynamicIndex);

            return dynamicIndex;
        }

        /// <summary>
        ///     Get an item out of the <see cref="MapSchema{T}" /> by it's index
        /// </summary>
        /// <param name="index">The index of the item</param>
        /// <returns>An object of type <typeparamref name="T" /> if it exists</returns>
        public object GetByIndex(int index)
        {
            string dynamicIndex = (string) GetIndex(index);
            return dynamicIndex != null && store.TryGet(dynamicIndex, out T value)
                ? value
                : GetTypeDefaultValue();
        }

        /// <summary>
        ///     Remove an item and it's dynamic index reference
        /// </summary>
        /// <param name="index">The index of the item</param>
        public void DeleteByIndex(int index)
        {
            string dynamicIndex = (string) GetIndex(index);
            if (
                //
                // FIXME:
                // The schema encoder should not encode a DELETE operation when using ADD + DELETE in the same key. (in the same patch)
                //
                dynamicIndex != null &&
                store.Remove(dynamicIndex)
            )
            {
                Indexes.Remove(index);
            }
        }

        /// <summary>
        ///     Resync sweep (see <c>Decoder.DecodeResync</c>): remove every entry
        ///     whose KEY the snapshot did not visit — maps prune by string key,
        ///     NOT wire index (the decoder-side <see cref="Indexes" /> journal
        ///     never evicts stale index→key mappings on re-indexing). Also scrubs
        ///     ALL index→key rows of swept keys.
        /// </summary>
        public void ResyncPrune(HashSet<object> visited, Action<object, object> prune, Action<object> keep)
        {
            List<string> deletedKeys = null;
            foreach (var entry in this)
            {
                if (visited.Contains(entry.Key)) { keep(entry.Value); continue; }
                (deletedKeys ??= new List<string>()).Add(entry.Key);
                prune(entry.Value, entry.Key);
            }

            if (deletedKeys != null)
            {
                foreach (string key in deletedKeys)
                {
                    store.Remove(key);

                    List<int> staleIndexes = null;
                    foreach (var kv in Indexes)
                    {
                        if (kv.Value == key) { (staleIndexes ??= new List<int>()).Add(kv.Key); }
                    }
                    if (staleIndexes != null)
                    {
                        foreach (int i in staleIndexes) { Indexes.Remove(i); }
                    }
                }
            }
        }

        /// <summary>
        ///     A new wrapper over the SAME entries and index journal — what the
        ///     decoder installs when a collection is (re-)sent.
        /// </summary>
        public ISchemaCollection Clone()
        {
            return new MapSchema<T>(store, Indexes);
        }

        /// <summary>
        ///     Determine what type of item this <see cref="MapSchema{T}" /> contains
        /// </summary>
        /// <returns>
        ///     <code>typeof(<typeparamref name="T" />);</code>
        /// </returns>
        public System.Type GetChildType()
        {
            return typeof(T);
        }

        /// <summary>
        ///     Get the default value of <typeparamref name="T" />
        /// </summary>
        /// <returns>
        ///     <code>default(<typeparamref name="T" />);</code>
        /// </returns>
        public object GetTypeDefaultValue()
        {
            return default(T);
        }

        /// <summary>Whether the map holds <paramref name="key" />.</summary>
        public bool ContainsKey(string key)
        {
            return store.Slots.ContainsKey(key);
        }

        /// <summary>
        ///     Determine if this <see cref="MapSchema{T}" /> contains <paramref name="key" />
        /// </summary>
        /// <param name="key">The key that will be checked for</param>
        /// <returns>True if the map contains the <paramref name="key" />, false if not</returns>
        public bool ContainsKey(object key)
        {
            return key is string s && store.Slots.ContainsKey(s);
        }

        /// <summary>
        ///     Getter for <see cref="HasSchemaChild" />
        ///     <para>This calls: <code>Schema.CheckSchemaChild(typeof(T))</code></para>
        /// </summary>
        public bool HasSchemaChild { get; } = Schema.CheckSchemaChild(typeof(T));

        /// <summary>
        ///     Getter/Setter of the <see cref="Type.ChildPrimitiveType" /> that this <see cref="MapSchema{T}" />
        ///     contains
        /// </summary>
        public string ChildPrimitiveType { get; set; }

        /// <summary>
        ///     Accessor to get/set an entry with a <paramref name="key" />
        /// </summary>
        /// <param name="key"></param>
        public object this[object key]
        {
            get { return this[(string) key]; }
            set { store.Set((string) key, HasSchemaChild ? (T) value : (T) Convert.ChangeType(value, typeof(T))); }
        }

        /// <summary>
        ///     The entries as a non-generic <see cref="IDictionary" /> (enumerates
        ///     <see cref="DictionaryEntry" />).
        /// </summary>
        public IEnumerable GetItems()
        {
            return LegacyView;
        }

        /// <summary>
        ///     Clear all items and indices
        /// </summary>
        /// <param name="refs">Passed in for garbage collection, if needed</param>
        public void Clear(List<DataChange> changes, ReferenceTracker refs)
        {
			Callbacks.RemoveChildRefs(this, changes, refs);
			Indexes.Clear();
            store.Clear();
        }

		public void Reverse()
		{
			throw new NotImplementedException();
		}

        /// <summary>
        ///     The number of entries in this <see cref="MapSchema{T}" />
        /// </summary>
        public int Count
        {
            get { return store.Count; }
        }

        /// <summary>
        ///     <b>UNIMPLEMENTED</b> setter function
        /// </summary>
        public void SetItems(object items) //TODO: Is it ok if this is unimplemented?
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Set <paramref name="item" />'s key to its value
        /// </summary>
        /// <param name="item">The new entry</param>
        public void Add(KeyValuePair<string, T> item)
        {
            store.Set(item.Key, item.Value);
        }

        /// <summary>
        ///     Check if <paramref name="item" />'s key exists in the map
        /// </summary>
        /// <param name="item">The entry whose key to check for</param>
        /// <returns>True if the key is found, false otherwise</returns>
        public bool Contains(KeyValuePair<string, T> item)
        {
            return store.Slots.ContainsKey(item.Key);
        }

        /// <summary>
        ///     Remove an item from this <see cref="MapSchema{T}" />
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>
        ///     True if <paramref name="item" /> was found and removed, false if it was not in the map to begin with
        /// </returns>
        public bool Remove(KeyValuePair<string, T> item)
        {
            T value;
            if (TryGetValue(item.Key, out value) && Equals(value, item.Value))
            {
                Remove(item.Key);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Add a new entry. Throws if <paramref name="key" /> is already present.
        /// </summary>
        /// <param name="key">The field name</param>
        /// <param name="value">The data to add</param>
        public void Add(string key, T value)
        {
            if (key == null) { throw new ArgumentNullException(nameof(key)); }
            if (store.Slots.ContainsKey(key))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));
            }
            store.Set(key, value);
        }

        public bool Remove(string key)
        {
            return store.Remove(key);
        }

        public bool TryGetValue(string key, out T value)
        {
            return store.TryGet(key, out value);
        }

        /// <summary>
        ///     Function to iterate over the entries and perform an <see cref="Action{T}" /> upon each one
        /// </summary>
        /// <param name="action">The <see cref="Action" /> to perform</param>
        public void ForEach(Action<string, T> action)
        {
            foreach (var entry in this) { action(entry.Key, entry.Value); }
        }

        public void ForEach(Action<object, object> action)
        {
            foreach (var entry in this) { action(entry.Key, entry.Value); }
        }

        /// <summary>Allocation-free, insertion-ordered enumeration of the entries.</summary>
        public Enumerator GetEnumerator() => new Enumerator(store);

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        ///     Walks the live slots in order. Adding or removing an entry while
        ///     enumerating throws, like the BCL collections.
        /// </summary>
        public struct Enumerator : IEnumerator<KeyValuePair<string, T>>
        {
            private readonly Store store;
            private readonly int version;
            private int slot;

            internal Enumerator(object store)
            {
                this.store = (Store)store;
                version = this.store.Version;
                slot = -1;
            }

            public KeyValuePair<string, T> Current
            {
                get
                {
                    ref var entry = ref store.Entries[slot];
                    return new KeyValuePair<string, T>(entry.Key, entry.Value);
                }
            }

            internal string CurrentKey => store.Entries[slot].Key;
            internal T CurrentValue => store.Entries[slot].Value;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (version != store.Version)
                {
                    throw new InvalidOperationException("MapSchema was modified during enumeration.");
                }
                while (++slot < store.End)
                {
                    if (store.Entries[slot].Key != null) { return true; }
                }
                slot = store.End;
                return false;
            }

            public void Reset()
            {
                if (version != store.Version)
                {
                    throw new InvalidOperationException("MapSchema was modified during enumeration.");
                }
                slot = -1;
            }

            public void Dispose() { }
        }

        /// <summary>The map's keys, in insertion order — a live view.</summary>
        public sealed class KeyCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly MapSchema<T> map;

            internal KeyCollection(MapSchema<T> map) { this.map = map; }

            public int Count => map.Count;

            public Enumerator GetEnumerator() => new Enumerator(map.store);
            IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(string item) => item != null && map.ContainsKey(item);

            public void CopyTo(string[] array, int arrayIndex)
            {
                foreach (var key in this) { array[arrayIndex++] = key; }
            }

            public void CopyTo(Array array, int index)
            {
                foreach (var key in this) { array.SetValue(key, index++); }
            }

            bool ICollection<string>.IsReadOnly => true;
            void ICollection<string>.Add(string item) => throw new NotSupportedException("MapSchema.Keys is read-only.");
            void ICollection<string>.Clear() => throw new NotSupportedException("MapSchema.Keys is read-only.");
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException("MapSchema.Keys is read-only.");
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public struct Enumerator : IEnumerator<string>
            {
                private MapSchema<T>.Enumerator inner;
                internal Enumerator(object store) { inner = new MapSchema<T>.Enumerator(store); }
                public string Current => inner.CurrentKey;
                object IEnumerator.Current => Current;
                public bool MoveNext() => inner.MoveNext();
                public void Reset() => inner.Reset();
                public void Dispose() { }
            }
        }

        /// <summary>The map's values, in insertion order — a live view.</summary>
        public sealed class ValueCollection : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly MapSchema<T> map;

            internal ValueCollection(MapSchema<T> map) { this.map = map; }

            public int Count => map.Count;

            public Enumerator GetEnumerator() => new Enumerator(map.store);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(T item)
            {
                var comparer = EqualityComparer<T>.Default;
                foreach (var value in this) { if (comparer.Equals(value, item)) { return true; } }
                return false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                foreach (var value in this) { array[arrayIndex++] = value; }
            }

            public void CopyTo(Array array, int index)
            {
                foreach (var value in this) { array.SetValue(value, index++); }
            }

            bool ICollection<T>.IsReadOnly => true;
            void ICollection<T>.Add(T item) => throw new NotSupportedException("MapSchema.Values is read-only.");
            void ICollection<T>.Clear() => throw new NotSupportedException("MapSchema.Values is read-only.");
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException("MapSchema.Values is read-only.");
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public struct Enumerator : IEnumerator<T>
            {
                private MapSchema<T>.Enumerator inner;
                internal Enumerator(object store) { inner = new MapSchema<T>.Enumerator(store); }
                public T Current => inner.CurrentValue;
                object IEnumerator.Current => Current;
                public bool MoveNext() => inner.MoveNext();
                public void Reset() => inner.Reset();
                public void Dispose() { }
            }
        }

        /// <summary>Backs the obsolete <see cref="items" /> property: a live non-generic view.</summary>
        private sealed class LegacyItems : IDictionary
        {
            private readonly MapSchema<T> map;

            public LegacyItems(MapSchema<T> map) { this.map = map; }

            // OrderedDictionary semantics: an int key is a position, anything else a key.
            public object this[object key]
            {
                get
                {
                    if (key is int index) { return map.store.Entries[map.store.SlotAt(index)].Value; }
                    return map.store.TryGet((string)key, out T value) ? (object)value : null;
                }
                set
                {
                    if (key is int index) { map.store.Entries[map.store.SlotAt(index)].Value = (T)value; return; }
                    map.store.Set((string)key, (T)value);
                }
            }

            public ICollection Keys => map.Keys;
            public ICollection Values => map.Values;
            public bool IsReadOnly => false;
            public bool IsFixedSize => false;
            public int Count => map.Count;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public void Add(object key, object value) => map.Add((string)key, (T)value);
            public void Clear() => map.store.Clear();
            public bool Contains(object key) => map.ContainsKey(key);
            public void Remove(object key) => map.Remove((string)key);

            public void CopyTo(Array array, int index)
            {
                foreach (var entry in map) { array.SetValue(new DictionaryEntry(entry.Key, entry.Value), index++); }
            }

            public IDictionaryEnumerator GetEnumerator() => new EntryEnumerator(map.GetEnumerator());
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class EntryEnumerator : IDictionaryEnumerator
            {
                private MapSchema<T>.Enumerator inner;
                public EntryEnumerator(MapSchema<T>.Enumerator inner) { this.inner = inner; }
                public DictionaryEntry Entry => new DictionaryEntry(inner.CurrentKey, inner.CurrentValue);
                public object Key => inner.CurrentKey;
                public object Value => inner.CurrentValue;
                public object Current => Entry;
                public bool MoveNext() => inner.MoveNext();
                public void Reset() => inner.Reset();
            }
        }
    }
}

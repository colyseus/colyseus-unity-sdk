using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	[Serializable, DebuggerDisplay("{Count}")]
	public class IndexedDictionary<KeyT, ValueT> : IDictionary<KeyT, ValueT>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private readonly Dictionary<KeyT, ValueT> dictionary;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly List<KeyT> keys;
		[DebuggerBrowsable(DebuggerBrowsableState.Never), NonSerialized]
		private ReadOnlyCollection<KeyT> keysReadOnly;

		public int Count { get { return this.dictionary.Count; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ReadOnlyCollection<KeyT> Keys { get { if (keysReadOnly == null) this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys); return this.keysReadOnly; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Dictionary<KeyT, ValueT>.ValueCollection Values { get { return this.dictionary.Values; } }
		public ValueT this[KeyT key]
		{
			get { return this.dictionary[key]; }
			set
			{
				if (this.dictionary.ContainsKey(key) == false)
					this.keys.Add(key);
				this.dictionary[key] = value;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection<KeyValuePair<KeyT, ValueT>>.IsReadOnly { get { return false; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection<KeyT> IDictionary<KeyT, ValueT>.Keys { get { return this.keysReadOnly; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection<ValueT> IDictionary<KeyT, ValueT>.Values { get { return this.dictionary.Values; } }

		public IndexedDictionary()
		{
			this.dictionary = new Dictionary<KeyT, ValueT>();
			this.keys = new List<KeyT>();
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}
		public IndexedDictionary(int count)
		{
			if (count < 0) throw new ArgumentOutOfRangeException("count");

			if (count == 0) count = 30;

			this.dictionary = new Dictionary<KeyT, ValueT>(count);
			this.keys = new List<KeyT>(count);
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}
		public IndexedDictionary(IDictionary<KeyT, ValueT> dictionary)
		{
			if (dictionary == null) throw new ArgumentNullException("dictionary");

			this.dictionary = new Dictionary<KeyT, ValueT>(dictionary);
			this.keys = new List<KeyT>(dictionary.Keys);
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}
		public IndexedDictionary(IEnumerable<KeyValuePair<KeyT, ValueT>> pairs)
		{
			if (pairs == null) throw new ArgumentNullException("pairs");

			this.dictionary = new Dictionary<KeyT, ValueT>();
			this.keys = new List<KeyT>();
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);

			foreach (var pair in pairs)
				this.Add(pair.Key, pair.Value);
		}
		public IndexedDictionary(IDictionary<KeyT, ValueT> dictionary, ICollection<KeyT> keys)
		{
			if (dictionary == null) throw new ArgumentNullException("dictionary");
			if (keys == null) throw new ArgumentNullException("keys");


			this.dictionary = new Dictionary<KeyT, ValueT>(dictionary);
			this.keys = new List<KeyT>(keys);
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}

		public void Add(KeyT key, ValueT value)
		{
			this.dictionary.Add(key, value);
			this.keys.Add(key);
		}
		public void Add(IndexedDictionary<KeyT, ValueT> other)
		{
			if (other == null) throw new ArgumentNullException("other");

			if (this.Count == 0)
			{
				this.keys.AddRange(other.keys);
				foreach (var kv in other.dictionary)
					this.dictionary.Add(kv.Key, kv.Value);
			}
			else
			{
				foreach (var kv in other.dictionary)
				{
					this.dictionary.Add(kv.Key, kv.Value);
					this.keys.Add(kv.Key);
				}
			}
		}

		public void Insert(int index, KeyT key, ValueT value)
		{
			// Dictionary operation first, so exception thrown if key already exists.
			this.dictionary.Add(key, value);
			this.keys.Insert(index, key);
		}
		public bool ContainsKey(KeyT key)
		{
			return this.dictionary.ContainsKey(key);
		}
		public bool ContainsKey(KeyT key, IEqualityComparer<KeyT> keyComparer)
		{
			foreach (var k in this.keys)
				if (keyComparer.Equals(k, key))
					return true;
			return false;
		}
		public bool ContainsValue(ValueT value)
		{
			foreach (var kv in this.dictionary)
				if (Equals(value, kv.Value))
					return true;
			return false;
		}
		public bool ContainsValue(ValueT value, IEqualityComparer comparer)
		{
			if (comparer == null) throw new ArgumentNullException("comparer");

			foreach (var kv in this.dictionary)
				if (comparer.Equals(value, kv.Value))
					return true;
			return false;
		}
		public bool Remove(KeyT key)
		{
			var wasInDictionary = this.dictionary.Remove(key);
			this.keys.Remove(key);

			return wasInDictionary;
		}

		public bool TryGetValue(KeyT key, out ValueT value)
		{
			return this.dictionary.TryGetValue(key, out value);
		}

		public int IndexOf(KeyT key)
		{
			return this.keys.IndexOf(key);
		}
		public void RemoveAt(int index)
		{
			if (index >= this.Count || index < 0) throw new ArgumentOutOfRangeException("index");

			var key = this.keys[index];
			this.dictionary.Remove(key);
			this.keys.RemoveAt(index);
		}
		public void SortKeys(IComparer<KeyT> comparer)
		{
			if (comparer == null) throw new ArgumentNullException("comparer");

			this.keys.Sort(comparer);
		}

		public void Clear()
		{
			this.dictionary.Clear();
			this.keys.Clear();
		}

		void ICollection<KeyValuePair<KeyT, ValueT>>.Add(KeyValuePair<KeyT, ValueT> item)
		{
			this.Add(item.Key, item.Value);
		}
		bool ICollection<KeyValuePair<KeyT, ValueT>>.Contains(KeyValuePair<KeyT, ValueT> item)
		{
			var value = default(ValueT);
			return this.dictionary.TryGetValue(item.Key, out value) && Equals(value, item.Value);
		}
		void ICollection<KeyValuePair<KeyT, ValueT>>.CopyTo(KeyValuePair<KeyT, ValueT>[] array, int arrayIndex)
		{
			foreach (var pair in this)
			{
				array[arrayIndex] = pair;
				arrayIndex++;
			}

		}
		bool ICollection<KeyValuePair<KeyT, ValueT>>.Remove(KeyValuePair<KeyT, ValueT> item)
		{
			if (!this.Contains(item))
				return false;

			return this.Remove(item.Key);
		}
		IEnumerator<KeyValuePair<KeyT, ValueT>> IEnumerable<KeyValuePair<KeyT, ValueT>>.GetEnumerator()
		{
			return GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		public override string ToString()
		{
			return this.Count.ToString();
		}

		public struct Enumerator : IEnumerator<KeyValuePair<KeyT, ValueT>>
		{
			private List<KeyT>.Enumerator innerEnumerator;
			private readonly IndexedDictionary<KeyT, ValueT> _dictionary;
			private KeyValuePair<KeyT, ValueT> current;

			public Enumerator(IndexedDictionary<KeyT, ValueT> dictionary)
			{
				this._dictionary = dictionary;
				this.innerEnumerator = dictionary.keys.GetEnumerator();
				this.current = new KeyValuePair<KeyT, ValueT>();
			}

			public KeyValuePair<KeyT, ValueT> Current { get { return this.current; } }
			object IEnumerator.Current { get { return this.Current; } }

			public bool MoveNext()
			{
				if (!this.innerEnumerator.MoveNext())
					return false;

				var key = this.innerEnumerator.Current;
				this.current = new KeyValuePair<KeyT, ValueT>(key, _dictionary.dictionary[key]);
				return true;
			}
			public void Reset()
			{
				this.innerEnumerator = _dictionary.keys.GetEnumerator();
			}
			public void Dispose()
			{
				this.innerEnumerator.Dispose();
			}
		}
	}
}

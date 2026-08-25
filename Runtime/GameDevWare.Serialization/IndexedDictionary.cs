using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	/// <summary>
	/// Represents a collection of keys and values that are accessible by key and by index, maintaining the insertion order of elements.
	/// <para>While the JSON specification typically treats objects as unordered collections of name/value pairs, 
	/// and the standard .NET <see cref="Dictionary{TKey, TValue}"/> does not guarantee a specific iteration order, 
	/// this class ensures that the order of keys is preserved during serialization and deserialization.</para>
	/// <para>Unlike standard JavaScript objects, which automatically sort numeric keys in ascending order, 
	/// this collection maintains the exact insertion order regardless of the key's type or value. 
	/// Use this to ensure that data structures remain predictable and consistent across all platforms.</para>
	/// </summary>
	/// <typeparam name="KeyT">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="ValueT">The type of the values in the dictionary.</typeparam>
	[Serializable, DebuggerDisplay("IndexedDictionary, Count: {Count}")]
	public class IndexedDictionary<KeyT, ValueT> : IDictionary<KeyT, ValueT>, IDictionary
	{
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private readonly Dictionary<KeyT, ValueT> dictionary;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly List<KeyT> keys;
		[DebuggerBrowsable(DebuggerBrowsableState.Never), NonSerialized]
		private ReadOnlyCollection<KeyT> keysReadOnly;

		/// <inheritdoc />
		public int Count { get { return this.dictionary.Count; } }

		/// <inheritdoc />
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

		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object IDictionary.this[object key]
		{
			get
			{
				var value = default(ValueT);
				return this.TryGetValue((KeyT)key, out value) ? value : default(object);
			}
			set { this[(KeyT)key] = (ValueT)value; }
		}
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object ICollection.SyncRoot { get { return this.dictionary; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized { get { return false; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ReadOnlyCollection<KeyT> Keys { get { return this.keysReadOnly ?? (this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys)); } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ReadOnlyCollection<ValueT> Values { get { return this.GetValues(); } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection IDictionary.Values { get { return this.GetValues(); } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection IDictionary.Keys { get { return this.keys; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IDictionary.IsReadOnly { get { return false; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IDictionary.IsFixedSize { get { return false; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection<KeyValuePair<KeyT, ValueT>>.IsReadOnly { get { return false; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection<KeyT> IDictionary<KeyT, ValueT>.Keys { get { return this.keysReadOnly; } }
		/// <inheritdoc />
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ICollection<ValueT> IDictionary<KeyT, ValueT>.Values { get { return this.GetValues(); } }

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedDictionary{KeyT, ValueT}"/> class.
		/// </summary>
		public IndexedDictionary()
		{
			this.dictionary = new Dictionary<KeyT, ValueT>();
			this.keys = new List<KeyT>();
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedDictionary{KeyT, ValueT}"/> class with the specified initial capacity.
		/// </summary>
		/// <param name="count">The initial number of elements that the <see cref="IndexedDictionary{KeyT, ValueT}"/> can contain.</param>
		public IndexedDictionary(int count)
		{
			if (count < 0) throw new ArgumentOutOfRangeException("count");

			if (count == 0) count = 30;

			this.dictionary = new Dictionary<KeyT, ValueT>(count);
			this.keys = new List<KeyT>(count);
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedDictionary{KeyT, ValueT}"/> class that contains elements copied from the specified dictionary.
		/// </summary>
		/// <param name="dictionary">The dictionary whose elements are copied to the new <see cref="IndexedDictionary{KeyT, ValueT}"/>.</param>
		public IndexedDictionary(IDictionary<KeyT, ValueT> dictionary)
		{
			if (dictionary == null) throw new ArgumentNullException("dictionary");

			this.dictionary = new Dictionary<KeyT, ValueT>(dictionary);
			this.keys = new List<KeyT>(dictionary.Keys);
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedDictionary{KeyT, ValueT}"/> class that contains elements copied from the specified collection.
		/// </summary>
		/// <param name="pairs">The collection whose elements are copied to the new <see cref="IndexedDictionary{KeyT, ValueT}"/>.</param>
		public IndexedDictionary(IEnumerable<KeyValuePair<KeyT, ValueT>> pairs)
		{
			if (pairs == null) throw new ArgumentNullException("pairs");

			this.dictionary = new Dictionary<KeyT, ValueT>();
			this.keys = new List<KeyT>();
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);

			foreach (var pair in pairs)
				this.Add(pair.Key, pair.Value);
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedDictionary{KeyT, ValueT}"/> class with the specified dictionary and keys.
		/// </summary>
		/// <param name="dictionary">The dictionary whose elements are copied to the new <see cref="IndexedDictionary{KeyT, ValueT}"/>.</param>
		/// <param name="keys">The collection of keys that determines the order of elements in the new <see cref="IndexedDictionary{KeyT, ValueT}"/>.</param>
		public IndexedDictionary(IDictionary<KeyT, ValueT> dictionary, ICollection<KeyT> keys)
		{
			if (dictionary == null) throw new ArgumentNullException("dictionary");
			if (keys == null) throw new ArgumentNullException("keys");


			this.dictionary = new Dictionary<KeyT, ValueT>(dictionary);
			this.keys = new List<KeyT>(keys);
			this.keysReadOnly = new ReadOnlyCollection<KeyT>(this.keys);
		}

		/// <inheritdoc />
		public void Add(KeyT key, ValueT value)
		{
			this.dictionary.Add(key, value);
			this.keys.Add(key);
		}
		/// <inheritdoc />
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

		/// <inheritdoc />
		public void Insert(int index, KeyT key, ValueT value)
		{
			// Dictionary operation first, so exception thrown if key already exists.
			this.dictionary.Add(key, value);
			this.keys.Insert(index, key);
		}
		/// <inheritdoc />
		public bool ContainsKey(KeyT key)
		{
			return this.dictionary.ContainsKey(key);
		}
		/// <inheritdoc />
		public bool ContainsKey(KeyT key, IEqualityComparer<KeyT> keyComparer)
		{
			foreach (var k in this.keys)
				if (keyComparer.Equals(k, key))
					return true;
			return false;
		}
		/// <inheritdoc />
		public bool ContainsValue(ValueT value)
		{
			foreach (var kv in this.dictionary)
				if (Equals(value, kv.Value))
					return true;
			return false;
		}
		/// <inheritdoc />
		public bool ContainsValue(ValueT value, IEqualityComparer comparer)
		{
			if (comparer == null) throw new ArgumentNullException("comparer");

			foreach (var kv in this.dictionary)
				if (comparer.Equals(value, kv.Value))
					return true;
			return false;
		}
		/// <inheritdoc />
		public bool Remove(KeyT key)
		{
			var wasInDictionary = this.dictionary.Remove(key);
			this.keys.Remove(key);

			return wasInDictionary;
		}

		/// <inheritdoc />
		public bool TryGetValue(KeyT key, out ValueT value)
		{
			return this.dictionary.TryGetValue(key, out value);
		}

		/// <inheritdoc />
		public int IndexOf(KeyT key)
		{
			return this.keys.IndexOf(key);
		}
		/// <inheritdoc />
		public void RemoveAt(int index)
		{
			if (index >= this.Count || index < 0) throw new ArgumentOutOfRangeException("index");

			var key = this.keys[index];
			this.dictionary.Remove(key);
			this.keys.RemoveAt(index);
		}
		/// <summary>
		/// Sorts the keys in the <see cref="IndexedDictionary{KeyT, ValueT}"/> using the specified comparer.
		/// </summary>
		/// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing keys, or null to use the default comparer.</param>
		public void SortKeys(IComparer<KeyT> comparer)
		{
			if (comparer == null) throw new ArgumentNullException("comparer");

			this.keys.Sort(comparer);
		}

		/// <inheritdoc />
		public void Clear()
		{
			this.dictionary.Clear();
			this.keys.Clear();
		}

		private ReadOnlyCollection<ValueT> GetValues()
		{
			var values = new ValueT[this.Count];
			var index = 0;
			foreach (var key in this.keys)
				values[index++] = this.dictionary[key];
			return new ReadOnlyCollection<ValueT>(values);
		}

		/// <inheritdoc />
		bool IDictionary.Contains(object key)
		{
			return this.ContainsKey((KeyT)key);
		}
		/// <inheritdoc />
		void IDictionary.Add(object key, object value)
		{
			this.Add((KeyT)key, (ValueT)value);
		}
		/// <inheritdoc />
		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		/// <inheritdoc />
		void IDictionary.Remove(object key)
		{
			this.Remove((KeyT)key);
		}
		/// <inheritdoc />
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null) throw new ArgumentNullException("array");
			if (index >= array.Length) throw new ArgumentOutOfRangeException("index");
			if (index + this.Count > array.Length) throw new ArgumentOutOfRangeException("index");

			var end = index + this.Count;
			for (var i = 0; i < end; i++)
				array.SetValue(new DictionaryEntry(this.keys[i], this.dictionary[this.keys[i]]), index + i);
		}
		/// <inheritdoc />
		void ICollection<KeyValuePair<KeyT, ValueT>>.Add(KeyValuePair<KeyT, ValueT> item)
		{
			this.Add(item.Key, item.Value);
		}
		/// <inheritdoc />
		bool ICollection<KeyValuePair<KeyT, ValueT>>.Contains(KeyValuePair<KeyT, ValueT> item)
		{
			var value = default(ValueT);
			return this.dictionary.TryGetValue(item.Key, out value) && Equals(value, item.Value);
		}
		/// <inheritdoc />
		void ICollection<KeyValuePair<KeyT, ValueT>>.CopyTo(KeyValuePair<KeyT, ValueT>[] array, int arrayIndex)
		{
			foreach (var pair in this)
			{
				array[arrayIndex] = pair;
				arrayIndex++;
			}

		}
		/// <inheritdoc />
		bool ICollection<KeyValuePair<KeyT, ValueT>>.Remove(KeyValuePair<KeyT, ValueT> item)
		{
			if (!this.Contains(item))
				return false;

			return this.Remove(item.Key);
		}
		/// <inheritdoc />
		IEnumerator<KeyValuePair<KeyT, ValueT>> IEnumerable<KeyValuePair<KeyT, ValueT>>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the <see cref="IndexedDictionary{KeyT, ValueT}"/>.
		/// </summary>
		/// <returns>An <see cref="Enumerator"/> for the <see cref="IndexedDictionary{KeyT, ValueT}"/>.</returns>
		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "Count: " + this.Count.ToString();
		}

		/// <summary>
		/// Enumerates the elements of an <see cref="IndexedDictionary{KeyT, ValueT}"/>.
		/// </summary>
		public struct Enumerator : IEnumerator<KeyValuePair<KeyT, ValueT>>, IDictionaryEnumerator
		{
			private List<KeyT>.Enumerator innerEnumerator;
			private readonly IndexedDictionary<KeyT, ValueT> owner;
			private KeyValuePair<KeyT, ValueT> current;

			/// <summary>
			/// Initializes a new instance of the <see cref="Enumerator"/> struct.
			/// </summary>
			/// <param name="owner">The <see cref="IndexedDictionary{KeyT, ValueT}"/> to enumerate.</param>
			public Enumerator(IndexedDictionary<KeyT, ValueT> owner)
			{
				this.owner = owner;
				this.innerEnumerator = owner.keys.GetEnumerator();
				this.current = new KeyValuePair<KeyT, ValueT>();
			}

			/// <inheritdoc />
			public object Key { get { return this.current.Key; } }
			/// <inheritdoc />
			public object Value { get { return this.current.Value; } }
			/// <inheritdoc />
			public DictionaryEntry Entry { get { return new DictionaryEntry(this.current.Key, this.current.Value); } }
			/// <inheritdoc />
			public KeyValuePair<KeyT, ValueT> Current { get { return this.current; } }
			/// <inheritdoc />
			object IEnumerator.Current { get { return this.Entry; } }

			/// <inheritdoc />
			public bool MoveNext()
			{
				if (!this.innerEnumerator.MoveNext())
					return false;

				var key = this.innerEnumerator.Current;
				this.current = new KeyValuePair<KeyT, ValueT>(key, this.owner.dictionary[key]);
				return true;
			}
			/// <inheritdoc />
			public void Reset()
			{
				this.innerEnumerator = this.owner.keys.GetEnumerator();
			}
			/// <inheritdoc />
			public void Dispose()
			{
				this.innerEnumerator.Dispose();
			}
		}
	}
}

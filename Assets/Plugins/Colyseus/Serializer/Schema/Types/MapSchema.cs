using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	public class MapSchema<T> : ISchemaCollection
	{
		public OrderedDictionary Items = new OrderedDictionary();
		public event KeyValueEventHandler<T, string> OnAdd;
		public event KeyValueEventHandler<T, string> OnChange;
		public event KeyValueEventHandler<T, string> OnRemove;
		private bool _hasSchemaChild = Schema.CheckSchemaChild(typeof(T));

		protected Dictionary<int, string> Indexes = new Dictionary<int, string>();

		public int __refId { get; set; }

		public MapSchema()
		{
			Items = new OrderedDictionary();
		}

		public MapSchema(OrderedDictionary items = null)
		{
			Items = items ?? new OrderedDictionary();
		}

		public void SetIndex(int index, object dynamicIndex)
		{
			Indexes[index] = (string) dynamicIndex;
		}

		public void SetByIndex(int index, object dynamicIndex, object value)
		{
			Indexes[index] = (string)dynamicIndex;
			Items[dynamicIndex] = (T)value;
		}

		public object GetIndex(int index)
		{
			string dynamicIndex;

			Indexes.TryGetValue(index, out dynamicIndex);

			return dynamicIndex;
		}

		public object GetByIndex(int index)
		{
			string dynamicIndex = (string) GetIndex(index);
			return (dynamicIndex != null && Items.Contains(dynamicIndex))
				? Items[dynamicIndex]
				: GetTypeDefaultValue();
		}

		public void DeleteByIndex(int index)
		{
			string dynamicIndex = (string) GetIndex(index);
			if (Items.Contains(dynamicIndex))
			{
				Items.Remove(dynamicIndex);
				Indexes.Remove(index);
			}
		}

		public ISchemaCollection Clone()
		{
			var clone = new MapSchema<T>(Items)
			{
				OnAdd = OnAdd,
				OnChange = OnChange,
				OnRemove = OnRemove
			};
			return clone;
		}

		public System.Type GetChildType()
		{
			return typeof(T);
		}

		public object GetTypeDefaultValue()
		{
			return default(T);
		}

		public bool ContainsKey(object key)
		{
			return Items.Contains(key);
		}

		public bool HasSchemaChild
		{
			get { return _hasSchemaChild; }
		}

		public string ChildPrimitiveType { get; set; }

		public T this[string key]
		{
			get
			{
				T value;
				TryGetValue(key, out value);
				return value;
			}
			set { Items[key] = value; }
		}

		public object this[object key]
		{
			get { return this[(string)key]; }
			set { Items[(string)key] = (HasSchemaChild) ? (T)value : (T)Convert.ChangeType(value, typeof(T)); }
		}

		public IDictionary GetItems()
		{
			return Items;
		}

		public void Add(KeyValuePair<string, T> item)
		{
			Items[item.Key] = item.Value;
		}

		public void Clear(ReferenceTracker refs = null)
		{
			if (refs != null && HasSchemaChild)
			{
				foreach (IRef item in Items.Values)
				{
					refs.Remove(item.__refId);
				}
			}

			Indexes.Clear();
			Items.Clear();
		}

		public bool Contains(KeyValuePair<string, T> item)
		{
			return Items.Contains(item.Key);
		}

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

		public int Count
		{
			get { return Items.Count; }
		}

		public bool ContainsKey(string key)
		{
			return Items.Contains(key);
		}

		public void Add(string key, T value)
		{
			Items.Add(key, value);
		}

		public bool Remove(string key)
		{
			var result = Items.Contains(key);
			if (result)
			{
				Items.Remove(key);
			}
			return result;
		}

		public bool TryGetValue(string key, out T value)
		{
			object foundValue;
			if ((foundValue = Items[key]) != null || Items.Contains(key))
			{
				// Either found with a non-null value, or contained value is null.
				value = (T)foundValue;
				return true;
			}
			value = default(T);
			return false;
		}

		public ICollection Keys
		{
			get { return Items.Keys; }
		}

		public ICollection Values
		{
			get { return Items.Values; }
		}

		public void SetItems(object items)
		{
			throw new NotImplementedException();
		}

		public void ForEach(Action<string, T> action)
		{
			foreach (DictionaryEntry item in Items)
			{
				action((string)item.Key, (T)item.Value);
			}
		}

		public void TriggerAll()
		{
			if (OnAdd == null) { return; }
			foreach (DictionaryEntry item in Items)
			{
				OnAdd.Invoke((T)item.Value, (string)item.Key);
			}
		}

		public void MoveEventHandlers(IRef previousInstance)
		{
			OnAdd = ((MapSchema<T>)previousInstance).OnAdd;
			OnChange = ((MapSchema<T>)previousInstance).OnChange;
			OnRemove = ((MapSchema<T>)previousInstance).OnRemove;
		}

		public void InvokeOnAdd(object item, object index)
		{
			OnAdd?.Invoke((T)item, (string)index);
		}

		public void InvokeOnChange(object item, object index)
		{
			OnChange?.Invoke((T)item, (string)index);
		}

		public void InvokeOnRemove(object item, object index)
		{
			OnRemove?.Invoke((T)item, (string)index);
		}
	}
}
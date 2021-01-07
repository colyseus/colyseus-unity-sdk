using System;
using System.Collections;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	public class ArraySchema<T> : ISchemaCollection
	{
		public Dictionary<int, T> Items;
		public event KeyValueEventHandler<T, int> OnAdd;
		public event KeyValueEventHandler<T, int> OnChange;
		public event KeyValueEventHandler<T, int> OnRemove;
		private bool _hasSchemaChild = Schema.CheckSchemaChild(typeof(T));

		protected Dictionary<int, int> Indexes = new Dictionary<int, int>();

		public int __refId { get; set; }

		public ArraySchema()
		{
			Items = new Dictionary<int, T>();
		}

		public ArraySchema(Dictionary<int, T> items = null)
		{
			Items = items ?? new Dictionary<int, T>();
		}

		public void SetIndex(int index, object dynamicIndex)
		{
			Indexes[index] = (int)dynamicIndex;
		}

		public void SetByIndex(int index, object dynamicIndex, object value)
		{
			Items[(int)dynamicIndex] = (T)value;
		}

		public object GetIndex(int index)
		{
			return (Indexes.ContainsKey(index))
				? Indexes[index]
				: -1;
		}

		public object GetByIndex(int index)
		{
			int dynamicIndex = (int) GetIndex(index);

			if (dynamicIndex != -1)
			{
				T value;
				Items.TryGetValue(dynamicIndex, out value);
				return value;

			} else
			{
				return null;
			}
		}

		public void DeleteByIndex(int index)
		{
			Items.Remove((int)GetIndex(index));
			Indexes.Remove(index);
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

		public ISchemaCollection Clone()
		{
			var clone = new ArraySchema<T>(Items)
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
			return Items.ContainsKey((int)key);
		}

		public bool HasSchemaChild
		{
			get { return _hasSchemaChild; }
		}

		public string ChildPrimitiveType { get; set; }

		public int Count
		{
			get { return Items.Count; }
		}

		public T this[int index]
		{
			get { return (T) GetByVirtualIndex(index); }
			set { Items[index] = value; }
		}

		public object this[object key]
		{
			get { return this[(int)key]; }
			set { Items[(int)key] = (HasSchemaChild) ? (T)value : (T)Convert.ChangeType(value, typeof(T)); }
		}

		public IDictionary GetItems()
		{
			return Items;
		}

		public void SetItems(object items)
		{
			Items = (Dictionary<int, T>)items;
		}

		public void ForEach(Action<T> action)
		{
			foreach (KeyValuePair<int, T> item in Items)
			{
				action(item.Value);
			}
		}

		public void TriggerAll()
		{
			if (OnAdd == null) { return; }
			for (var i = 0; i < Items.Count; i++)
			{
				OnAdd.Invoke((T)Items[i], (int)i);
			}
		}

		public void MoveEventHandlers(IRef previousInstance)
		{
			OnAdd = ((ArraySchema<T>)previousInstance).OnAdd;
			OnChange = ((ArraySchema<T>)previousInstance).OnChange;
			OnRemove = ((ArraySchema<T>)previousInstance).OnRemove;
		}

		public void InvokeOnAdd(object item, object index)
		{
			OnAdd?.Invoke((T)item, (int)index);
		}

		public void InvokeOnChange(object item, object index)
		{
			OnChange?.Invoke((T)item, (int)index);
		}

		public void InvokeOnRemove(object item, object index)
		{
			OnRemove?.Invoke((T)item, (int)index);
		}

		protected T GetByVirtualIndex (int index)
		{
			//
			// FIXME: should be O(1)
			//
			var keys = new List<int>(Items.Keys);

			int dynamicIndex = (index < keys.Count)
				? keys[index]
				: -1;

			T value;
			Items.TryGetValue(dynamicIndex, out value);

			return value;
		}
	}
}
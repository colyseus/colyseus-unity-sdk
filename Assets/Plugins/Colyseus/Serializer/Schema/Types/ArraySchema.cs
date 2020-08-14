using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Colyseus.Schema
{
	public class ArraySchema<T> : ISchemaCollection, IRef
	{
		public Dictionary<int, T> Items;
		public event KeyValueEventHandler<T, int> OnAdd;
		public event KeyValueEventHandler<T, int> OnChange;
		public event KeyValueEventHandler<T, int> OnRemove;
		private bool _hasSchemaChild = Schema.CheckSchemaChild(typeof(T));

		protected Dictionary<int, int> Indexes = new Dictionary<int, int>();

		public int __refId { get; set; }
		public  IRef __parent { get; set; }

		public ArraySchema()
		{
			Items = new Dictionary<int, T>();
		}

		public ArraySchema(Dictionary<int, T> items = null)
		{
			Items = items ?? new Dictionary<int, T>();
		}

		public void SetIndex(int index, dynamic dynamicIndex)
		{
			if (!Indexes.ContainsKey(index))
			{
				Indexes.Add(index, dynamicIndex);
			}
		}

		public void SetByIndex(int index, object dynamicIndex, object value)
		{
			Items.Add((int)dynamicIndex, (T)value);
		}

		public dynamic GetIndex(int index)
		{
			int dynamicIndex;

			Indexes.TryGetValue(index, out dynamicIndex);

			return dynamicIndex;
		}

		public object GetByIndex(int index)
		{
			// TODO:
			T value;
			Items.TryGetValue(index, out value);
			return value;
		}

		public void DeleteByIndex(int index)
		{
			// TODO:
			Items.Remove(index);
		}

		public void Clear()
		{
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

		public dynamic GetTypeDefaultValue()
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
			get
			{
				T value;
				Items.TryGetValue(index, out value);
				return value;
			}
			set { Items[index] = value; }
		}

		public object this[object key]
		{
			get
			{
				T value;
				Items.TryGetValue((int)key, out value);
				return value;
			}
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

		public void MoveEventHandlers(ISchemaCollection previousInstance)
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
	}
}
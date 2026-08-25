using System;
using System.Collections;
using System.Collections.Generic;
using Colyseus;

namespace Colyseus.Schema
{
	/// <summary>
	///     A <see cref="Schema" /> array of <typeparamref name="T" /> type objects
	/// </summary>
	/// <typeparam name="T">The type of object in this array</typeparam>
	public class ArraySchema<T> : IArraySchema
	{
		/// <summary>
		///     The contents of the <see cref="ArraySchema{T}" />
		/// </summary>
		public List<T> items;

		internal HashSet<int> deletedKeys = new HashSet<int>();

		[Preserve]
		public ArraySchema()
		{
			items = new List<T>();
		}

		public ArraySchema(List<T> items = null)
		{
			this.items = items ?? new List<T>();
		}

		/// <summary>
		///     Accessor to get/set <see cref="items" /> by index
		/// </summary>
		public T this[int index]
		{
			get { return items[index]; }
			set { items[index] = value; }
		}

		/// <inheritdoc />
		public int __refId { get; set; }

		public void SetByIndex(int index, object value, byte operation)
		{
			bool wasDeleted = deletedKeys.Remove(index); // consult + clear in one call

			// strict ADD only: MOVE_AND_ADD/DELETE_AND_ADD/ADD_BY_REFID must not insert
			if (
				operation == (byte)OPERATION.ADD &&
				index < items.Count &&
				!wasDeleted
			)
			{
				// ADD at an occupied index = insert: shift existing items up.
				items.Insert(index, (T)value);
			}
			else if (operation == (byte)OPERATION.DELETE_AND_MOVE)
			{
				items.RemoveAt(index);

				for (int i = items.Count; i <= index; i++)
				{
					items.Add(default(T));
				}

				items[index] = (T)value;
				// items.Insert(index, (T) value);
			}
			else
			{
				for (int i = items.Count; i <= index; i++)
				{
					items.Add(default(T));
				}

				items[index] = (T)value;
				// items.Insert(index, (T) value);
			}
		}

		/// <summary>
		///     Get an item out of the <see cref="ArraySchema{T}" /> by it's index
		/// </summary>
		/// <param name="index">The index of the item</param>
		/// <returns>An object of type <typeparamref name="T" /> if it exists</returns>
		public object GetByIndex(int index)
		{
			try
			{
				return items[index];
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}
		}

		/// <summary>
		///     Remove an item and it's dynamic index reference
		/// </summary>
		/// <param name="index">The index of the item</param>
		public void DeleteByIndex(int index)
		{
			deletedKeys.Add(index);

			// skip if index is out of range
			if (index >= items.Count)
			{
				return;
			}

			items[index] = default(T);
		}

		/// <summary>
		///     Clear all items and indices
		/// </summary>
		/// <param name="refs">Passed in for garbage collection, if needed</param>
		public void Clear(List<DataChange> changes, ReferenceTracker refs)
		{
			Callbacks.RemoveChildRefs(this, changes, refs);
			items.Clear();
		}

		public void Reverse()
		{
			items.Reverse();
		}

		/// <summary>
		///     Clone this <see cref="ArraySchema{T}" />
		/// </summary>
		/// <returns>A copy of this <see cref="ArraySchema{T}" /></returns>
		public ISchemaCollection Clone()
		{
			ArraySchema<T> clone = new ArraySchema<T>(items);
			return clone;
		}

		/// <summary>
		///     Determine what type of item this <see cref="ArraySchema{T}" /> contains
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

		/// <summary>
		///     Getter for <see cref="HasSchemaChild" />
		///     <para>This calls: <code>Schema.CheckSchemaChild(typeof(T))</code></para>
		/// </summary>
		public bool HasSchemaChild { get; } = Schema.CheckSchemaChild(typeof(T));

		/// <summary>
		///     Getter/Setter of the <see cref="Type.ChildPrimitiveType" /> that this <see cref="ArraySchema{T}" />
		///     contains
		/// </summary>
		public string ChildPrimitiveType { get; set; }

		/// <summary>
		///     Getter for the amount of <see cref="items" /> in this <see cref="ArraySchema{T}" />
		/// </summary>
		public int Count
		{
			get { return items.Count; }
		}

		/// <summary>
		///     Accessor to get/set <see cref="items" /> with a <paramref name="key" />
		/// </summary>
		/// <param name="key"></param>
		public object this[object key]
		{
			get { return this[(int)key]; }
			set { items[(int)key] = HasSchemaChild ? (T)value : (T)Convert.ChangeType(value, typeof(T)); }
		}

		/// <summary>
		///     Getter function to get all the <see cref="items" /> in this <see cref="ArraySchema{T}" />
		/// </summary>
		/// <returns>
		///     <see cref="items" />
		/// </returns>
		public IEnumerable GetItems()
		{
			return items;
		}

		public int IndexOf(T value)
		{
			int i = 0;
			foreach (var item in items)
			{
				if (item.Equals(value))
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		/// <summary>
		///     Setter function to cast and set <see cref="items" />
		/// </summary>
		/// <param name="items">
		///     The items to pass to the <see cref="ArraySchema{T}" />. Will be cast to Dictionary{int,
		///     <typeparamref name="T" />}
		/// </param>
		public void SetItems(object items)
		{
			this.items = (List<T>)items;
		}

		/// <summary>
		///     Function to iterate over <see cref="items" /> and perform an <see cref="Action{T}" /> upon each entry
		/// </summary>
		/// <param name="action">The <see cref="Action" /> to perform</param>
		public void ForEach(Action<int, T> action)
		{
			int i = 0;
			items.ForEach((value) =>
			{
				action(i, value);
				i++;
			});
		}

		public void ForEach(Action<object, object> action)
		{
			int i = 0;
			items.ForEach((value) =>
			{
				action(i, value);
				i++;
			});
		}

		public void OnDecodeEnd()
		{
			if (deletedKeys.Count == 0)
			{
				return;
			}

			var newItems = new List<T>();

			for (int i = 0; i < items.Count; i++)
			{
				if (deletedKeys.Contains(i))
				{
					continue;
				}
				newItems.Add(items[i]);
			}

			items = newItems;

			deletedKeys.Clear();
		}

		/// <summary>
		///     Resync sweep (see <c>Decoder.DecodeResync</c>): remove every entry
		///     whose index the snapshot did not visit. <c>items</c> is hole-free
		///     here (decode-end compaction already ran; full-sync emits dense
		///     ADDs). Visited indexes may be sparse — ADD_BY_REFID resolves to
		///     the current client-side index.
		/// </summary>
		public void ResyncPrune(HashSet<object> visited, Action<object, object> prune, Action<object> keep)
		{
			bool removed = false;
			for (int i = 0; i < items.Count; i++)
			{
				object value = items[i];
				if (visited.Contains(i)) { keep(value); continue; }
				removed = true;
				prune(value, i);
				DeleteByIndex(i);
			}
			if (removed) { OnDecodeEnd(); } // compact the holes
		}

	}
}

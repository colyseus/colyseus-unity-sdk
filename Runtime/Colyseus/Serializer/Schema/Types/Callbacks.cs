using System;
using System.Collections;
using System.Collections.Generic;

namespace Colyseus.Schema
{
    /// <summary>
    ///     Delegate function when any property on the schema structure has changed.
    /// </summary>
    public delegate void OnChangeEventHandler();


    /// <summary>
    ///     Delegate function for handling <see cref="Schema" /> removal
    /// </summary>
    public delegate void OnRemoveEventHandler();

    /// <summary>
    ///     Delegate for handling events given a <paramref name="key" /> and a <paramref name="value" />
    /// </summary>
    /// <param name="value">The affected value</param>
    /// <param name="key">The key we're affecting</param>
    /// <typeparam name="K">The type of <see cref="object" /> we're attempting to access</typeparam>
	/// <typeparam name="T">The <see cref="Schema" /> type</typeparam>
    public delegate void KeyValueEventHandler<K, T>(K key, T value);

    /// <summary>
    ///     Delegate for handling events given a <paramref name="key" /> and a <paramref name="value" />
    /// </summary>
    /// <param name="value">The affected value</param>
    /// <param name="key">The key we're affecting</param>
    /// <typeparam name="K">The type of <see cref="object" /> we're attempting to access</typeparam>
    /// <typeparam name="T">The <see cref="Schema" /> type</typeparam>
    public delegate void PropertyChangeHandler<T>(T currentValue, T previousValue);

    public class CollectionSchemaCallbacks<K, T>
	{
        public event KeyValueEventHandler<K, T> OnAdd;
        public event KeyValueEventHandler<K, T> OnChange;
        public event KeyValueEventHandler<K, T> OnRemove;

        public static void RemoveChildRefs(ISchemaCollection collection, ref List<DataChange> changes, ref ColyseusReferenceTracker refs)
        {
            if (refs == null) { return; }

            bool needRemoveRef = (collection.HasSchemaChild);

            foreach (DictionaryEntry item in collection.GetItems())
            {
                changes.Add(new DataChange {
                    RefId = collection.__refId,
                    Op = (byte) OPERATION.DELETE,
                    //Field = item.Key,
                    DynamicIndex = item.Key,
                    Value = null,
                    PreviousValue = item.Value
                });

                if (needRemoveRef)
                {
                    refs.Remove((item.Value as IRef).__refId);
                }
            }
        }

        /// <summary>
        ///     Trigger <see cref="OnAdd" /> with a specific <paramref name="item" /> at an <paramref name="index" />
        /// </summary>
        /// <param name="item">The item to add, will be cast to <typeparamref name="T" /> </param>
        /// <param name="index">The index of the item, will be cast to <see cref="string" /></param>
        public void InvokeOnAdd(object item, object index)
        {
            OnAdd?.Invoke((K)index, (T)item);
        }

        /// <summary>
        ///     Trigger <see cref="OnChange" /> with a specific <paramref name="item" /> at an <paramref name="index" />
        /// </summary>
        /// <param name="item">The item to change, will be cast to <typeparamref name="T" /> </param>
        /// <param name="index">The index of the item, will be cast to <see cref="string" /></param>
        public void InvokeOnChange(object item, object index)
        {
            OnChange?.Invoke((K)index, (T)item);
        }

        /// <summary>
        ///     Trigger <see cref="OnRemove" /> with a specific <paramref name="item" /> at an <paramref name="index" />
        /// </summary>
        /// <param name="item">The item to remove, will be cast to <typeparamref name="T" /> </param>
        /// <param name="index">The index of the item, will be cast to <see cref="string" /></param>
        public void InvokeOnRemove(object item, object index)
        {
            OnRemove?.Invoke((K)index, (T)item);
        }
    }

    public class SchemaCallbacks
	{
        /// <inheritdoc cref="OnChangeEventHandler" />
		public event OnChangeEventHandler OnChange;

        /// <inheritdoc cref="OnRemoveEventHandler" />
        public event OnRemoveEventHandler OnRemove;

        //public Dictionary<string, int> PropertyCallbacks = new Dictionary<string, int>();
        public Dictionary<string, int> PropertyCallbacks = new Dictionary<string, int>();

        /// <summary>
        ///     Trigger <see cref="OnChange" />
        /// </summary>
        public void InvokeOnChange()
        {
            OnChange?.Invoke();
        }

        /// <summary>
        ///     Trigger <see cref="OnRemove" />
        /// </summary>
        public void InvokeOnRemove()
        {
            OnRemove?.Invoke();
        }

		public void AddPropertyCallback(string property)
		{
            if (!PropertyCallbacks.ContainsKey(property))
            {
                PropertyCallbacks[property] = 0;
            }

			PropertyCallbacks[property]++;
		}

        public void RemovePropertyCallback(string property)
        {
			if ((--PropertyCallbacks[property]) == 0)
			{
				PropertyCallbacks.Remove(property);
			}

		}

        public bool HasPropertyCallback(string property)
		{
            return PropertyCallbacks.ContainsKey(property);

        }

    }

}
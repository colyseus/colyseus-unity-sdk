using System;
using System.Collections;
using System.Collections.Generic;

namespace Colyseus.Schema
{
    /// <summary>
    ///     A <see cref="Schema" /> array of <typeparamref name="T" /> type objects
    /// </summary>
    /// <typeparam name="T">The type of object in this array</typeparam>
    public class ArraySchema<T> : ISchemaCollection
    {
        public CollectionSchemaCallbacks<int, T> __callbacks = null;

        /// <summary>
        ///     Map of dynamic indices for quick access of <see cref="items" />
        /// </summary>
        protected Dictionary<int, int> indexes = new Dictionary<int, int>();

        /// <summary>
        ///     The contents of the <see cref="ArraySchema{T}" />
        /// </summary>
        public Dictionary<int, T> items;

        public ArraySchema()
        {
            items = new Dictionary<int, T>();
        }

        public ArraySchema(Dictionary<int, T> items = null)
        {
            this.items = items ?? new Dictionary<int, T>();
        }

        /// <summary>
        ///     Accessor to get/set <see cref="items" /> by index
        /// </summary>
        public T this[int index]
        {
            get { return GetByVirtualIndex(index); }
            set { items[index] = value; }
        }

        /// <inheritdoc />
        public int __refId { get; set; }

        /// <summary>
        ///     Set the <see cref="indexes" /> value
        /// </summary>
        /// <param name="index">The field index</param>
        /// <param name="dynamicIndex">The new dynamic Index value, cast to <see cref="int" /></param>
        public void SetIndex(int index, object dynamicIndex)
        {
            indexes[index] = (int) dynamicIndex;
        }

        /// <summary>
        ///     Set an Item by it's <paramref name="dynamicIndex" />
        /// </summary>
        /// <param name="index">Unused, only here to satisfy <see cref="IRef" /> parameters</param>
        /// <param name="dynamicIndex">
        ///     The index, cast to <see cref="int" />, in <see cref="items" /> that will be set to
        ///     <paramref name="value" />
        /// </param>
        /// <param name="value">The new object to put into <see cref="items" /></param>
        public void SetByIndex(int index, object dynamicIndex, object value)
        {
            items[(int) dynamicIndex] = (T) value;
        }

        /// <summary>
        ///     Get the dynamic index value from <see cref="indexes" />
        /// </summary>
        /// <param name="index">The location of the dynamic index to return</param>
        /// <returns>The dynamic index from <see cref="indexes" />, if it exists. -1 if it does not</returns>
        public object GetIndex(int index)
        {
            return indexes.ContainsKey(index)
                ? indexes[index]
                : -1;
        }

        /// <summary>
        ///     Get an item out of the <see cref="ArraySchema{T}" /> by it's index
        /// </summary>
        /// <param name="index">The index of the item</param>
        /// <returns>An object of type <typeparamref name="T" /> if it exists</returns>
        public object GetByIndex(int index)
        {
            int dynamicIndex = (int) GetIndex(index);

            if (dynamicIndex != -1)
            {
                T value;
                items.TryGetValue(dynamicIndex, out value);
                return value;
            }

            return null;
        }

        /// <summary>
        ///     Remove an item and it's dynamic index reference
        /// </summary>
        /// <param name="index">The index of the item</param>
        public void DeleteByIndex(int index)
        {
            items.Remove((int) GetIndex(index));
            indexes.Remove(index);
        }

        /// <summary>
        ///     Clear all items and indices
        /// </summary>
        /// <param name="refs">Passed in for garbage collection, if needed</param>
        public void Clear(ref List<DataChange> changes, ref ColyseusReferenceTracker refs)
        {
            CollectionSchemaCallbacks<int, T>.RemoveChildRefs(this, ref changes, ref refs);
            indexes.Clear();
            items.Clear();
        }

        /// <summary>
        ///     Clone this <see cref="ArraySchema{T}" />
        /// </summary>
        /// <returns>A copy of this <see cref="ArraySchema{T}" /></returns>
        public ISchemaCollection Clone()
        {
            ArraySchema<T> clone = new ArraySchema<T>(items)
            {
                __callbacks = __callbacks,
            };
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
        ///     Determine if this <see cref="ArraySchema{T}" /> contains <paramref name="key" />
        /// </summary>
        /// <param name="key">The key in <see cref="items" /> that will be cast to <see cref="int" /> and checked for</param>
        /// <returns>True if <see cref="items" /> contains the <paramref name="key" />, false if not</returns>
        public bool ContainsKey(object key)
        {
            return items.ContainsKey((int) key);
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
            get { return this[(int) key]; }
            set { items[(int) key] = HasSchemaChild ? (T) value : (T) Convert.ChangeType(value, typeof(T)); }
        }

        /// <summary>
        ///     Getter function to get all the <see cref="items" /> in this <see cref="ArraySchema{T}" />
        /// </summary>
        /// <returns>
        ///     <see cref="items" />
        /// </returns>
        public IDictionary GetItems()
        {
            return items;
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
            this.items = (Dictionary<int, T>) items;
        }

        /// <summary>
        ///     Attaches a callback that is triggered whenever a new item is received from the server
        /// </summary>
        /// <returns>An Action that, when called, removes the registered callback</returns>
        public Action OnAdd(KeyValueEventHandler<int, T> handler, bool triggerAll = true)
        {
            if (__callbacks == null) __callbacks = new CollectionSchemaCallbacks<int, T>();

            __callbacks.OnAdd += handler;

            if (triggerAll)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    __callbacks.InvokeOnAdd(items[i], i);
                }
            }

            return () => __callbacks.OnAdd -= handler;
        }

        /// <summary>
        ///     Attaches a callback that is triggered whenever a new item is received from the server
        /// </summary>
		/// <returns>An Action that, when called, removes the registered callback</returns>
        public Action OnChange(KeyValueEventHandler<int, T> handler)
        {
            if (__callbacks == null) __callbacks = new CollectionSchemaCallbacks<int, T>();

            __callbacks.OnChange += handler;

            return () => __callbacks.OnChange -= handler;
        }

        /// <summary>
        ///     Attaches a callback that is triggered whenever a new item is received from the server
        /// </summary>
		/// <returns>An Action that, when called, removes the registered callback</returns>
        public Action OnRemove(KeyValueEventHandler<int, T> handler)
        {
            if (__callbacks == null) __callbacks = new CollectionSchemaCallbacks<int, T>();

            __callbacks.OnRemove += handler;

            return () => __callbacks.OnRemove -= handler;
        }

        /// <summary>
        ///     Clone the Event Handlers from another <see cref="IRef" /> into this <see cref="ArraySchema{T}" />
        /// </summary>
        /// <param name="previousInstance">The <see cref="IRef" /> with the EventHandlers to copy</param>
        public void MoveEventHandlers(IRef previousInstance)
        {
            __callbacks = ((ArraySchema<T>)previousInstance).__callbacks;
        }

        public bool HasCallbacks() { return __callbacks != null; }
        public void InvokeOnAdd(object item, object index) { __callbacks?.InvokeOnAdd(item, index); }
        public void InvokeOnChange(object item, object index) { __callbacks?.InvokeOnChange(item, index); }
        public void InvokeOnRemove(object item, object index) { __callbacks?.InvokeOnRemove(item, index); }

        /// <summary>
        ///     Function to iterate over <see cref="items" /> and perform an <see cref="Action{T}" /> upon each entry
        /// </summary>
        /// <param name="action">The <see cref="Action" /> to perform</param>
        public void ForEach(Action<T> action)
        {
            foreach (KeyValuePair<int, T> item in items)
            {
                action(item.Value);
            }
        }

        /// <summary>
        ///     Get an object by the dynamic index stored in <see cref="indexes" /> at <paramref name="index" />
        /// </summary>
        /// <param name="index">The index of the object to get</param>
        /// <returns>The item at the dynamic index connected to the <paramref name="index" /> provided</returns>
        protected T GetByVirtualIndex(int index)
        {
            //
            // TODO: should be O(1)
            //
            List<int> keys = new List<int>(items.Keys);

            int dynamicIndex = index < keys.Count
                ? keys[index]
                : -1;

            T value;
            items.TryGetValue(dynamicIndex, out value);

            return value;
        }
    }
}

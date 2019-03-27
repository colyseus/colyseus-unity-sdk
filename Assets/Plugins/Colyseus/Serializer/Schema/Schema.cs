using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/***
  Allowed primitive types:
    "string"
    "number"
    "boolean"
    "int8"
    "uint8"
    "int16"
    "uint16"
    "int32"
    "uint32"
    "int64"
    "uint64"
    "float32"
    "float64"
       
  Allowed reference types:   
    "ref"
    "array"
    "map"
***/

namespace Colyseus.Schema
{
  [AttributeUsage(AttributeTargets.Field)]
  public class Type : Attribute
  {

    public string FieldType;
    public System.Type ChildType;

    public Type(string type, System.Type childType = null)
    {
      FieldType = type;
      ChildType = childType;
    }
  }

  public class Iterator { 
    public int Offset = 0;
  }

  public enum SPEC: byte
  {
    END_OF_STRUCTURE = 0xc1, // (msgpack spec: never used)
    NIL = 0xc0,
    INDEX_CHANGE = 0xd4,
  }

  public class DataChange
  {
    public string Field;
    public object Value;
    public object PreviousValue;
  }

  public class OnChangeEventArgs : EventArgs
  {
    public List<DataChange> Changes;
    public OnChangeEventArgs(List<DataChange> changes)
    {
      Changes = changes;
    }
  }

  public class KeyValueEventArgs<T, K> : EventArgs
  {
    public T Value;
    public K Key;

    public KeyValueEventArgs(T value, K key)
    {
      Value = value;
      Key = key;
    }
  }

  public interface ISchemaCollection
  {
    void InvokeOnAdd(object item, object index);
    void InvokeOnChange(object item, object index);
    void InvokeOnRemove(object item, object index);

    object CreateItemInstance();
    object GetItems();
    void SetItems(object items);

    bool HasSchemaChild { get; }
    int Count { get; }
    object this[object key] { get; set; }

    ISchemaCollection Clone();
  }

  public class ArraySchema<T> : ISchemaCollection
  {
    public List<T> Items;
    public event EventHandler<KeyValueEventArgs<T, int>> OnAdd;
    public event EventHandler<KeyValueEventArgs<T, int>> OnChange;
    public event EventHandler<KeyValueEventArgs<T, int>> OnRemove;

    public ArraySchema()
    {
      Items = new List<T>();
    }

    public ArraySchema(List<T> items = null)
    {
      Items = items ?? new List<T>();
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

    public object CreateItemInstance()
    {
      return (T) Activator.CreateInstance(typeof(T));
    }

    public bool HasSchemaChild
    {
      get { return typeof(T).BaseType == typeof(Schema); }
    }

    public int Count
    {
      get { return Items.Count; }
    }

    public T this[int index]
    {
      get {
        return (Items.Count > index) ? (T)Items[index] : default(T);
      }
      set { Items.Insert(index, value); }
    }

    public object this[object key]
    {
      get {
        int k = (int)key;
        return (Items.Count > k) ? (T)Items[k] : default(T);
      }
      set { Items.Insert((int)key, (T)value); }
    }

    public object GetItems()
    {
      return Items;
    }

    public void SetItems(object items)
    {
      Items = (List<T>) items;
    }

    public void InvokeOnAdd(object item, object index)
    {
      if (OnAdd != null) { OnAdd.Invoke(this, new KeyValueEventArgs<T, int>((T) item, (int) index)); }
    }

    public void InvokeOnChange(object item, object index)
    {
      if (OnChange != null) { OnChange.Invoke(this, new KeyValueEventArgs<T, int>((T) item, (int) index)); }
    }

    public void InvokeOnRemove(object item, object index)
    {
      if (OnRemove != null) { OnRemove.Invoke(this, new KeyValueEventArgs<T, int>((T) item, (int) index)); }
    }
  }

  public class MapSchema<T> : ISchemaCollection
  {
    public Dictionary<string, T> Items;
    public event EventHandler<KeyValueEventArgs<T, string>> OnAdd;
    public event EventHandler<KeyValueEventArgs<T, string>> OnChange;
    public event EventHandler<KeyValueEventArgs<T, string>> OnRemove;

    public MapSchema()
    {
      Items = new Dictionary<string, T>();
    }

    public MapSchema(Dictionary<string, T> items = null)
    {
      Items = items ?? new Dictionary<string, T>();
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

    public object CreateItemInstance()
    {
      return (T) Activator.CreateInstance(typeof(T));
    }

    public bool HasSchemaChild
    {
      get { return typeof(T) == typeof(Schema); }
    }

    public T this[string key]
    {
      get {
        T value;
        Items.TryGetValue(key, out value);
        return value;
      }
      set { Items[key] = value; }
    }

    public object this[object key]
    {
      get {
        T value;
        Items.TryGetValue(key as string, out value);
        return value;
      }
      set { Items[(string) key] = (T) value; }
    }

    public int Count
    {
      get { return Items.Count; }
    }

    public object GetItems()
    {
      return Items;
    }

    public void SetItems(object items)
    {
      throw new NotImplementedException();
    }

    public void InvokeOnAdd(object item, object index)
    {
      if (OnAdd != null) { OnAdd.Invoke(this, new KeyValueEventArgs<T, string>((T)item, (string)index)); }
    }

    public void InvokeOnChange(object item, object index)
    {
      if (OnChange != null) { OnChange.Invoke(this, new KeyValueEventArgs<T, string>((T)item, (string)index)); }
    }

    public void InvokeOnRemove(object item, object index)
    {
      if (OnRemove != null) { OnRemove.Invoke(this, new KeyValueEventArgs<T, string>((T)item, (string)index)); }
    }
  }

  public class Schema
  {
    protected Dictionary<int, string> fieldsByIndex = new Dictionary<int, string>();
    protected Dictionary<string, string> fieldTypes = new Dictionary<string, string>();
    protected Dictionary<string, System.Type> fieldChildTypes = new Dictionary<string, System.Type>();

    public event EventHandler<OnChangeEventArgs> OnChange;
    public event EventHandler OnRemove;

    public Schema()
    {
      int index = 0;

      FieldInfo[] fields = GetType().GetFields();
      foreach (FieldInfo field in fields)
      {
        object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);
		for (var i=0; i<typeAttributes.Length; i++)
		{
			Type t = (Type)typeAttributes[i];
			fieldsByIndex.Add(index++, field.Name);
			fieldTypes.Add(field.Name, t.FieldType);
			if (t.FieldType == "ref" || t.FieldType == "array" || t.FieldType == "map")
			{
				fieldChildTypes.Add(field.Name, t.ChildType);
			}
		}
      }
    }

    /* allow to retrieve property values by its string name */   
    public object this[string propertyName]
    {
      get { 
        return GetType().GetField(propertyName).GetValue(this); 
      }
      set {
      	var field = GetType().GetField(propertyName);
        field.SetValue(this, Convert.ChangeType(value, field.FieldType)); 
      }
    }

    public void Decode(byte[] bytes, Iterator it = null)
    {
      var decode = Decoder.GetInstance();

      if (it == null) { it = new Iterator(); }

      var changes = new List<DataChange>();
      var totalBytes = bytes.Length;

      while (it.Offset < totalBytes)
      {
        var index = bytes[it.Offset++];

        if (index == (byte) SPEC.END_OF_STRUCTURE)
        {
          break;
        }

        var field = fieldsByIndex[index];
        var fieldType = fieldTypes[field];
        System.Type childType;
        fieldChildTypes.TryGetValue(field, out childType);

        object value = null;

        object change = null;
        bool hasChange = false;

        if (fieldType == "ref")
        {
          // child schema type
          if (decode.NilCheck(bytes, it))
          {
            it.Offset++;
            value = null;
          }
          else
          {
            value = this[field] ?? Activator.CreateInstance(childType);
            (value as Schema).Decode(bytes, it);
          }

          hasChange = true;
        }

        // Array type
        else if (fieldType == "array")
        {
          change = new List<object>();

          ISchemaCollection valueRef = (ISchemaCollection)(this[field] ?? Activator.CreateInstance(childType));
          ISchemaCollection currentValue = valueRef.Clone();

          int newLength = Convert.ToInt32(decode.DecodeNumber(bytes, it));
          int numChanges = Convert.ToInt32(decode.DecodeNumber(bytes, it));

          hasChange = (numChanges > 0);

          bool hasIndexChange = false;

          // ensure current array has the same length as encoded one
          if (currentValue.Count > newLength)
          {
            for (var i = newLength; i < currentValue.Count; i++)
            {
              var item = currentValue[i];
              if (item is Schema && (item as Schema).OnRemove != null)
              {
                (item as Schema).OnRemove.Invoke(this, new EventArgs());
              }
              currentValue.InvokeOnRemove(item, i);
            }

            // reduce items length
            List<object> items = currentValue.GetItems() as List<object>;
            currentValue.SetItems(items.GetRange(0, newLength));
          }

          for (var i = 0; i < numChanges; i++)
          {
            var newIndex = Convert.ToInt32(decode.DecodeNumber(bytes, it));

            int indexChangedFrom = -1;
            if (decode.IndexChangeCheck(bytes, it))
            {
              decode.DecodeUint8(bytes, it);
              indexChangedFrom = Convert.ToInt32(decode.DecodeNumber(bytes, it));
              hasIndexChange = true;
            }

            var isNew = (!hasIndexChange && currentValue[newIndex] == null) || (hasIndexChange && indexChangedFrom != -1);

            if (currentValue.HasSchemaChild)
            {
              Schema item = null;

              if (isNew)
              {
                item = (Schema)currentValue.CreateItemInstance();

              }
              else if (indexChangedFrom != -1)
              {
                item = (Schema)valueRef[indexChangedFrom];
              }
              else
              {
                item = (Schema)valueRef[newIndex];
              }

              if (item == null)
              {
                item = (Schema)currentValue.CreateItemInstance();
                isNew = true;
              }

              if (decode.NilCheck(bytes, it))
              {
                it.Offset++;
                valueRef.InvokeOnRemove(item, newIndex);
                continue;
              }

              item.Decode(bytes, it);
              currentValue[newIndex] = item;
            }
            else
            {
              currentValue[newIndex] = decode.DecodePrimitiveType(fieldType, bytes, it);
            }

            if (isNew)
            {
              currentValue.InvokeOnAdd(currentValue[newIndex], newIndex);
            }
            else
            {
              currentValue.InvokeOnChange(currentValue[newIndex], newIndex);
            }

            (change as List<object>).Add(currentValue[newIndex]);
          }

          value = currentValue;
        }

        // Map type
        else if (fieldType == "map")
        {
          ISchemaCollection valueRef = (ISchemaCollection)(this[field] ?? Activator.CreateInstance(childType));
          ISchemaCollection currentValue = valueRef.Clone();

          int length = Convert.ToInt32(decode.DecodeNumber(bytes, it));
          hasChange = (length > 0);

          bool hasIndexChange = false;

          IDictionary items = currentValue.GetItems() as IDictionary;
          string[] mapKeys = new string[items.Keys.Count];
          items.Keys.CopyTo(mapKeys, 0);

          for (var i = 0; i < length; i++)
          {
            // `encodeAll` may indicate a higher number of indexes it actually encodes
            // TODO: do not encode a higher number than actual encoded entries
            if (it.Offset > bytes.Length || bytes[it.Offset] == (byte)SPEC.END_OF_STRUCTURE)
            {
              break;
            }

            string previousKey = null;
            if (decode.IndexChangeCheck(bytes, it))
            {
              it.Offset++;
              previousKey = mapKeys[Convert.ToInt32(decode.DecodeNumber(bytes, it))];
              hasIndexChange = true;
            }

            bool hasMapIndex = decode.NumberCheck(bytes, it);
            bool isSchemaType = childType != null;

            string newKey = (hasMapIndex)
                ? mapKeys[Convert.ToInt32(decode.DecodeNumber(bytes, it))]
                : decode.DecodeString(bytes, it);

            object item;
            bool isNew = (!hasIndexChange && valueRef[newKey] == null) || (hasIndexChange && previousKey == null && hasMapIndex);

            if (isNew && isSchemaType)
            {
              item = (Schema)currentValue.CreateItemInstance();

            } else if (previousKey != null)
            {
              item = valueRef[previousKey];
            }
            else
            {
              item = valueRef[newKey];
            }

            if (decode.NilCheck(bytes, it))
            {
              it.Offset++;

              if (item != null && (item as Schema).OnRemove != null)
              {
                (item as Schema).OnRemove.Invoke(this, new EventArgs());
              }

              valueRef.InvokeOnRemove(item, newKey);
              items.Remove(newKey);
              continue;

            } else if (!isSchemaType)
            {
              currentValue[newKey] = decode.DecodePrimitiveType(fieldType, bytes, it);
            }
            else
            {
              (item as Schema).Decode(bytes, it);
              currentValue[newKey] = item;
            }

            if (isNew)
            {
              currentValue.InvokeOnAdd(item, newKey);
            }
            else
            {
              currentValue.InvokeOnChange(item, newKey);
            }
          }

          value = currentValue;
        }

        // Primitive type
        else
        {
          value = decode.DecodePrimitiveType(fieldType, bytes, it);
          hasChange = true;
        }

        if (hasChange)
        {
          changes.Add(new DataChange
          {
            Field = field,
            Value = (change != null) ? change : value,
            PreviousValue = this[field]
          });
        }

        this[field] = value;
      }

      if (changes.Count > 0 && OnChange != null)
      {
        OnChange.Invoke(this, new OnChangeEventArgs(changes));
      }
    }
  }
}

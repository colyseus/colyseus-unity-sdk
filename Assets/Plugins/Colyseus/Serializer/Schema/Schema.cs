using System;
using System.Collections;
using System.Collections.Specialized;
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
  class Context
  {
    protected static Context instance = new Context();
    protected List<System.Type> types = new List<System.Type>();
    protected Dictionary<uint, System.Type> typeIds = new Dictionary<uint, System.Type>();

    public static Context GetInstance()
    {
      return instance;
    }

    public void SetTypeId(System.Type type, uint typeid)
    {
      typeIds[typeid] = type;
    }

    public System.Type Get(uint typeid)
    {
      return typeIds[typeid];
    }
  }

  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class Type : Attribute
  {
    public int Index;
    public string FieldType;
    public System.Type ChildType;
    public string ChildPrimitiveType;

    public Type(int index, string type, System.Type childType = null)
    {
      Index = index; // GetType().GetFields() doesn't guarantee order of fields, need to manually track them here!
      FieldType = type;
      ChildType = childType;
    }

    public Type(int index, string type, string childPrimitiveType)
    {
      Index = index; // GetType().GetFields() doesn't guarantee order of fields, need to manually track them here!
      FieldType = type;
      ChildPrimitiveType = childPrimitiveType;
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
    TYPE_ID = 0xd5
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

    object GetItems();
    void SetItems(object items);
    void TriggerAll();

    System.Type GetChildType();

    bool HasSchemaChild { get; }
    int Count { get; }
    object this[object key] { get; set; }

    ISchemaCollection Clone();
  }

  public class ArraySchema<T> : ISchemaCollection
  {
    public Dictionary<int, T> Items;
    public event EventHandler<KeyValueEventArgs<T, int>> OnAdd;
    public event EventHandler<KeyValueEventArgs<T, int>> OnChange;
    public event EventHandler<KeyValueEventArgs<T, int>> OnRemove;

    public ArraySchema()
    {
      Items = new Dictionary<int, T>();
    }

    public ArraySchema(Dictionary<int, T> items = null)
    {
      Items = items ?? new Dictionary<int, T>();
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
        T value;
        Items.TryGetValue(index, out value);
        return value;
      }
      set { Items[index] = value; }
    }

    public object this[object key]
    {
      get {
        T value;
        Items.TryGetValue((int)key, out value);
        return value;
      }
      set { Items[(int)key] = (HasSchemaChild) ? (T)value : (T)Convert.ChangeType(value, typeof(T)); }
    }

    public object GetItems()
    {
      return Items;
    }

    public void SetItems(object items)
    {
      Items = (Dictionary<int, T>) items;
    }

    public void ForEach (Action<T> action)
    {
      foreach (KeyValuePair<int, T> item in Items)
      {
        action(item.Value);
      }
    }

    public void TriggerAll()
    {
      if (OnAdd == null) { return; }
      for (var i = 0; i < Items.Count; i++) {
        OnAdd.Invoke(this, new KeyValueEventArgs<T, int>((T) Items[i], (int) i));
      }
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
    public OrderedDictionary Items = new OrderedDictionary();
    public event EventHandler<KeyValueEventArgs<T, string>> OnAdd;
    public event EventHandler<KeyValueEventArgs<T, string>> OnChange;
    public event EventHandler<KeyValueEventArgs<T, string>> OnRemove;

    public MapSchema()
    {
      Items = new OrderedDictionary();
    }

    public MapSchema(OrderedDictionary items = null)
    {
      Items = items ?? new OrderedDictionary();
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

    public bool HasSchemaChild
    {
      get { return typeof(T).BaseType == typeof(Schema); }
    }

    public T this[string key]
    {
      get {
        T value;
        TryGetValue(key, out value);
        return value;
      }
      set { Items[key] = value; }
    }

    public object this[object key]
    {
      get {
        T value;
        TryGetValue(key as string, out value);
        return value;
      }
      set { Items[(string)key] = (HasSchemaChild) ? (T)value : (T)Convert.ChangeType(value, typeof(T)); }
    }

    public object GetItems()
    {
      return Items;
    }

    public void Add(KeyValuePair<string, T> item)
    {
      Items[item.Key] = item.Value;
    }

    public void Clear()
    {
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

    public void ForEach (Action<string, T> action)
    {
      foreach (DictionaryEntry item in Items)
      {
        action((string)item.Key, (T)item.Value);
      }
    }

    public void TriggerAll()
    {
      if (OnAdd == null) { return; }
      foreach(DictionaryEntry item in Items)
      {
        OnAdd.Invoke(this, new KeyValueEventArgs<T, string>((T)item.Value, (string)item.Key));
      }
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
    protected Dictionary<string, string> fieldChildPrimitiveTypes = new Dictionary<string, string>();
    protected Dictionary<string, System.Type> fieldChildTypes = new Dictionary<string, System.Type>();

    public event EventHandler<OnChangeEventArgs> OnChange;
    public event EventHandler OnRemove;

    public Schema()
    {
      FieldInfo[] fields = GetType().GetFields();
      foreach (FieldInfo field in fields)
      {
        object[] typeAttributes = field.GetCustomAttributes(typeof(Type), true);
        for (var i=0; i<typeAttributes.Length; i++)
        {
          Type t = (Type)typeAttributes[i];
          fieldsByIndex.Add(t.Index, field.Name);
          fieldTypes.Add(field.Name, t.FieldType);
          if (t.FieldType == "ref" || t.FieldType == "array" || t.FieldType == "map")
          {
            if (t.ChildPrimitiveType != null)
            {
              fieldChildPrimitiveTypes.Add(field.Name, t.ChildPrimitiveType);
            }
            else
            {
              fieldChildTypes.Add(field.Name, t.ChildType);
            }
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
        field.SetValue(this, value);
      }
    }

    public void Decode(byte[] bytes, Iterator it = null)
    {
      var decode = Decoder.GetInstance();

      if (it == null) { it = new Iterator(); }

      var changes = new List<DataChange>();
      var totalBytes = bytes.Length;

      // skip TYPE_ID of existing instances
      if (bytes[it.Offset] == (byte) SPEC.TYPE_ID)
      {
        it.Offset += 2;
      }

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

        string childPrimitiveType;
        fieldChildPrimitiveTypes.TryGetValue(field, out childPrimitiveType);

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
            value = this[field] ?? CreateTypeInstance(bytes, it, childType);
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
          int numChanges = Math.Min(Convert.ToInt32(decode.DecodeNumber(bytes, it)), newLength);

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
                item = (Schema)CreateTypeInstance(bytes, it, currentValue.GetChildType());

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
                item = (Schema)CreateTypeInstance(bytes, it, currentValue.GetChildType());
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
              currentValue[newIndex] = decode.DecodePrimitiveType(childPrimitiveType, bytes, it);
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

          OrderedDictionary items = currentValue.GetItems() as OrderedDictionary;
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
              item = (Schema)CreateTypeInstance(bytes, it, currentValue.GetChildType());

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
              currentValue[newKey] = decode.DecodePrimitiveType(childPrimitiveType, bytes, it);
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

    public void TriggerAll()
    {
      if (OnChange == null) { return; }

      var changes = new List<DataChange>();
      foreach(KeyValuePair<int, string> entry in fieldsByIndex)
      {
        var field = entry.Value;
        if (this[field] != null)
        {
          changes.Add(new DataChange
          {
            Field = field,
            Value = this[field],
            PreviousValue = null
          });
        }
      }

      OnChange.Invoke(this, new OnChangeEventArgs(changes));
    }

    protected object CreateTypeInstance(byte[] bytes, Iterator it, System.Type type)
    {
      if (bytes[it.Offset] == (byte) SPEC.TYPE_ID)
      {
        it.Offset++;
        uint typeId = Decoder.GetInstance().DecodeUint8(bytes, it);
        System.Type anotherType = Context.GetInstance().Get(typeId);
        return Activator.CreateInstance(anotherType);
      }
      else
      {
        return Activator.CreateInstance(type);
      }
    }
  }

  public class ReflectionField : Schema
  {
    [Type(0, "string")]
    public string name;

    [Type(1, "string")]
    public string type;

    [Type(2, "uint8")]
    public uint referencedType;
  }

  public class ReflectionType : Schema
  {
    [Type(0, "uint8")]
    public uint id;

    [Type(1, "array", typeof(ArraySchema<ReflectionField>))]
    public ArraySchema<ReflectionField> fields = new ArraySchema<ReflectionField>();
  }

  public class Reflection : Schema
  {
    [Type(0, "array", typeof(ArraySchema<ReflectionType>))]
    public ArraySchema<ReflectionType> types = new ArraySchema<ReflectionType>();

    [Type(1, "uint8")]
    public uint rootType;
  }

}

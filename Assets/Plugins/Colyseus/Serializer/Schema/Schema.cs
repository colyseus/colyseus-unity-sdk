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

  public delegate void OnChangeEventHandler(List<DataChange> changes);
  public delegate void KeyValueEventHandler<T, K>(T value, K key);
  public delegate void OnRemoveEventHandler();

  public interface ISchemaCollection
  {
    void InvokeOnAdd(object item, object index);
    void InvokeOnChange(object item, object index);
    void InvokeOnRemove(object item, object index);

    IDictionary GetItems();
    void SetItems(object items);
    void TriggerAll();

    System.Type GetChildType();
    bool ContainsKey(object key);

    bool HasSchemaChild { get; }
    int Count { get; }
    object this[object key] { get; set; }

    ISchemaCollection Clone();
  }

  public class ArraySchema<T> : ISchemaCollection
  {
    public Dictionary<int, T> Items;
    public event KeyValueEventHandler<T, int> OnAdd;
    public event KeyValueEventHandler<T, int> OnChange;
    public event KeyValueEventHandler<T, int> OnRemove;
    private bool _hasSchemaChild = Schema.CheckSchemaChild(typeof(T));

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

    public bool ContainsKey(object key)
    {
      return Items.ContainsKey((int)key);
    }

    public bool HasSchemaChild
    {
      get { return _hasSchemaChild; }
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

    public IDictionary GetItems()
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
        OnAdd.Invoke((T) Items[i], (int) i);
      }
    }

    public void InvokeOnAdd(object item, object index)
    {
      OnAdd?.Invoke((T) item, (int) index);
    }

    public void InvokeOnChange(object item, object index)
    {
      OnChange?.Invoke((T) item, (int) index);
    }

    public void InvokeOnRemove(object item, object index)
    {
      OnRemove?.Invoke((T) item, (int) index);
    }
  }

  public class MapSchema<T> : ISchemaCollection
  {
    public OrderedDictionary Items = new OrderedDictionary();
    public event KeyValueEventHandler<T, string> OnAdd;
    public event KeyValueEventHandler<T, string> OnChange;
    public event KeyValueEventHandler<T, string> OnRemove;
    private bool _hasSchemaChild = Schema.CheckSchemaChild(typeof(T));

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

    public bool ContainsKey(object key)
    {
      return Items.Contains(key);
    }

    public bool HasSchemaChild
    {
      get { return _hasSchemaChild; }
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

    public IDictionary GetItems()
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
        OnAdd.Invoke((T)item.Value, (string)item.Key);
      }
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

  public class Schema
  {
    protected Dictionary<int, string> fieldsByIndex = new Dictionary<int, string>();
    protected Dictionary<string, string> fieldTypes = new Dictionary<string, string>();
    protected Dictionary<string, string> fieldChildPrimitiveTypes = new Dictionary<string, string>();
    protected Dictionary<string, System.Type> fieldChildTypes = new Dictionary<string, System.Type>();

    public event OnChangeEventHandler OnChange;
    public event OnRemoveEventHandler OnRemove;

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

      while (it.Offset < totalBytes)
      {
        // skip TYPE_ID of existing instances
        if (bytes[it.Offset] == (byte) SPEC.TYPE_ID)
        {
          it.Offset += 2;
        }

        var isNil = decode.NilCheck(bytes, it);
        if (isNil) { it.Offset++; }

        var index = bytes[it.Offset++];

        if (index == (byte) SPEC.END_OF_STRUCTURE)
        {
          break;
        }

        // Schema version mismatch (backwards compatibility)
        if (!fieldsByIndex.ContainsKey(index))
        {
          continue;
        }

        var field = fieldsByIndex[index];
        var fieldType = fieldTypes[field];

        System.Type childType;
        fieldChildTypes.TryGetValue(field, out childType);

        string childPrimitiveType;
        fieldChildPrimitiveTypes.TryGetValue(field, out childPrimitiveType);

        object value = null;

        bool hasChange = false;

        if (isNil)
        {
            value = null;
            hasChange = true;
        }

        // Child schema type
        else if (fieldType == "ref")
        {
          value = this[field] ?? CreateTypeInstance(bytes, it, childType);
          (value as Schema).Decode(bytes, it);

          hasChange = true;
        }

        // Array type
        else if (fieldType == "array")
        {
          ISchemaCollection valueRef = (ISchemaCollection)(this[field] ?? Activator.CreateInstance(childType));
          ISchemaCollection currentValue = valueRef.Clone();

          int newLength = Convert.ToInt32(decode.DecodeNumber(bytes, it));
          int numChanges = Math.Min(Convert.ToInt32(decode.DecodeNumber(bytes, it)), newLength);

          bool hasRemoval = (currentValue.Count > newLength);
          hasChange = (numChanges > 0) || hasRemoval;

          bool hasIndexChange = false;

          // ensure current array has the same length as encoded one
          if (hasRemoval)
          {
            IDictionary items = currentValue.GetItems();

            for (int i = newLength, l = currentValue.Count; i < l; i++)
            {
              var item = currentValue[i];
              if (item is Schema)
              {
                (item as Schema).OnRemove?.Invoke();
              }

              items.Remove(i);
              currentValue.InvokeOnRemove(item, i);
            }
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

            var isNew = (!hasIndexChange && !currentValue.ContainsKey(newIndex)) || (hasIndexChange && indexChangedFrom != -1);

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

            var isNilItem = decode.NilCheck(bytes, it);
            if (isNilItem) { it.Offset++; }

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
            bool isNew = (!hasIndexChange && !valueRef.ContainsKey(newKey)) || (hasIndexChange && previousKey == null && hasMapIndex);

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

            if (isNilItem)
            {
              if (item != null && isSchemaType)
              {
                (item as Schema).OnRemove?.Invoke();
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
              currentValue.InvokeOnAdd(currentValue[newKey], newKey);
            }
            else
            {
              currentValue.InvokeOnChange(currentValue[newKey], newKey);
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
            Value = value,
            PreviousValue = this[field]
          });
        }

        this[field] = value;
      }

      if (changes.Count > 0)
      {
        OnChange?.Invoke(changes);
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

      OnChange.Invoke(changes);
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

    public static bool CheckSchemaChild(System.Type toCheck) {
      System.Type generic = typeof(Schema);

      while (toCheck != null && toCheck != typeof(object)) {
        var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;

        if (generic == cur) {
          return true;
        }

        toCheck = toCheck.BaseType;
      }

      return false;
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

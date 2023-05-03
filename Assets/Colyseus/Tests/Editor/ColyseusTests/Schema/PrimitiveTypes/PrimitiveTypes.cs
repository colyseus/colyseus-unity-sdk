// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.PrimitiveTypes {
	public partial class PrimitiveTypes : Schema {
		[Type(0, "int8")]
		public sbyte int8 = default(sbyte);

		[Type(1, "uint8")]
		public byte uint8 = default(byte);

		[Type(2, "int16")]
		public short int16 = default(short);

		[Type(3, "uint16")]
		public ushort uint16 = default(ushort);

		[Type(4, "int32")]
		public int int32 = default(int);

		[Type(5, "uint32")]
		public uint uint32 = default(uint);

		[Type(6, "int64")]
		public long int64 = default(long);

		[Type(7, "uint64")]
		public ulong uint64 = default(ulong);

		[Type(8, "float32")]
		public float float32 = default(float);

		[Type(9, "float64")]
		public double float64 = default(double);

		[Type(10, "number")]
		public float varint_int8 = default(float);

		[Type(11, "number")]
		public float varint_uint8 = default(float);

		[Type(12, "number")]
		public float varint_int16 = default(float);

		[Type(13, "number")]
		public float varint_uint16 = default(float);

		[Type(14, "number")]
		public float varint_int32 = default(float);

		[Type(15, "number")]
		public float varint_uint32 = default(float);

		[Type(16, "number")]
		public float varint_int64 = default(float);

		[Type(17, "number")]
		public float varint_uint64 = default(float);

		[Type(18, "number")]
		public float varint_float32 = default(float);

		[Type(19, "number")]
		public float varint_float64 = default(float);

		[Type(20, "string")]
		public string str = default(string);

		[Type(21, "boolean")]
		public bool boolean = default(bool);

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<sbyte> _int8Change;
		public Action OnInt8Change(PropertyChangeHandler<sbyte> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(int8));
			_int8Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(int8));
				_int8Change -= handler;
			};
		}

		protected event PropertyChangeHandler<byte> _uint8Change;
		public Action OnUint8Change(PropertyChangeHandler<byte> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(uint8));
			_uint8Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(uint8));
				_uint8Change -= handler;
			};
		}

		protected event PropertyChangeHandler<short> _int16Change;
		public Action OnInt16Change(PropertyChangeHandler<short> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(int16));
			_int16Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(int16));
				_int16Change -= handler;
			};
		}

		protected event PropertyChangeHandler<ushort> _uint16Change;
		public Action OnUint16Change(PropertyChangeHandler<ushort> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(uint16));
			_uint16Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(uint16));
				_uint16Change -= handler;
			};
		}

		protected event PropertyChangeHandler<int> _int32Change;
		public Action OnInt32Change(PropertyChangeHandler<int> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(int32));
			_int32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(int32));
				_int32Change -= handler;
			};
		}

		protected event PropertyChangeHandler<uint> _uint32Change;
		public Action OnUint32Change(PropertyChangeHandler<uint> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(uint32));
			_uint32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(uint32));
				_uint32Change -= handler;
			};
		}

		protected event PropertyChangeHandler<long> _int64Change;
		public Action OnInt64Change(PropertyChangeHandler<long> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(int64));
			_int64Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(int64));
				_int64Change -= handler;
			};
		}

		protected event PropertyChangeHandler<ulong> _uint64Change;
		public Action OnUint64Change(PropertyChangeHandler<ulong> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(uint64));
			_uint64Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(uint64));
				_uint64Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _float32Change;
		public Action OnFloat32Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(float32));
			_float32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(float32));
				_float32Change -= handler;
			};
		}

		protected event PropertyChangeHandler<double> _float64Change;
		public Action OnFloat64Change(PropertyChangeHandler<double> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(float64));
			_float64Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(float64));
				_float64Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_int8Change;
		public Action OnVarint_int8Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_int8));
			_varint_int8Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_int8));
				_varint_int8Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_uint8Change;
		public Action OnVarint_uint8Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_uint8));
			_varint_uint8Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_uint8));
				_varint_uint8Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_int16Change;
		public Action OnVarint_int16Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_int16));
			_varint_int16Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_int16));
				_varint_int16Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_uint16Change;
		public Action OnVarint_uint16Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_uint16));
			_varint_uint16Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_uint16));
				_varint_uint16Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_int32Change;
		public Action OnVarint_int32Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_int32));
			_varint_int32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_int32));
				_varint_int32Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_uint32Change;
		public Action OnVarint_uint32Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_uint32));
			_varint_uint32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_uint32));
				_varint_uint32Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_int64Change;
		public Action OnVarint_int64Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_int64));
			_varint_int64Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_int64));
				_varint_int64Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_uint64Change;
		public Action OnVarint_uint64Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_uint64));
			_varint_uint64Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_uint64));
				_varint_uint64Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_float32Change;
		public Action OnVarint_float32Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_float32));
			_varint_float32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_float32));
				_varint_float32Change -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _varint_float64Change;
		public Action OnVarint_float64Change(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(varint_float64));
			_varint_float64Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(varint_float64));
				_varint_float64Change -= handler;
			};
		}

		protected event PropertyChangeHandler<string> _strChange;
		public Action OnStrChange(PropertyChangeHandler<string> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(str));
			_strChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(str));
				_strChange -= handler;
			};
		}

		protected event PropertyChangeHandler<bool> _booleanChange;
		public Action OnBooleanChange(PropertyChangeHandler<bool> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(boolean));
			_booleanChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(boolean));
				_booleanChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(int8): _int8Change?.Invoke((sbyte) change.Value, (sbyte) change.PreviousValue); break;
				case nameof(uint8): _uint8Change?.Invoke((byte) change.Value, (byte) change.PreviousValue); break;
				case nameof(int16): _int16Change?.Invoke((short) change.Value, (short) change.PreviousValue); break;
				case nameof(uint16): _uint16Change?.Invoke((ushort) change.Value, (ushort) change.PreviousValue); break;
				case nameof(int32): _int32Change?.Invoke((int) change.Value, (int) change.PreviousValue); break;
				case nameof(uint32): _uint32Change?.Invoke((uint) change.Value, (uint) change.PreviousValue); break;
				case nameof(int64): _int64Change?.Invoke((long) change.Value, (long) change.PreviousValue); break;
				case nameof(uint64): _uint64Change?.Invoke((ulong) change.Value, (ulong) change.PreviousValue); break;
				case nameof(float32): _float32Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(float64): _float64Change?.Invoke((double) change.Value, (double) change.PreviousValue); break;
				case nameof(varint_int8): _varint_int8Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_uint8): _varint_uint8Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_int16): _varint_int16Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_uint16): _varint_uint16Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_int32): _varint_int32Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_uint32): _varint_uint32Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_int64): _varint_int64Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_uint64): _varint_uint64Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_float32): _varint_float32Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(varint_float64): _varint_float64Change?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(str): _strChange?.Invoke((string) change.Value, (string) change.PreviousValue); break;
				case nameof(boolean): _booleanChange?.Invoke((bool) change.Value, (bool) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

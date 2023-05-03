// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.Callbacks {
	public partial class Container : Schema {
		[Type(0, "number")]
		public float num = default(float);

		[Type(1, "string")]
		public string str = default(string);

		[Type(2, "ref", typeof(Ref))]
		public Ref aRef = new Ref();

		[Type(3, "array", typeof(ArraySchema<Ref>))]
		public ArraySchema<Ref> arrayOfSchemas = new ArraySchema<Ref>();

		[Type(4, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> arrayOfNumbers = new ArraySchema<float>();

		[Type(5, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<float> _numChange;
		public Action OnNumChange(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(num));
			_numChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(num));
				_numChange -= handler;
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

		protected event PropertyChangeHandler<Ref> _aRefChange;
		public Action OnARefChange(PropertyChangeHandler<Ref> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(aRef));
			_aRefChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(aRef));
				_aRefChange -= handler;
			};
		}

		protected event PropertyChangeHandler<ArraySchema<Ref>> _arrayOfSchemasChange;
		public Action OnArrayOfSchemasChange(PropertyChangeHandler<ArraySchema<Ref>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(arrayOfSchemas));
			_arrayOfSchemasChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(arrayOfSchemas));
				_arrayOfSchemasChange -= handler;
			};
		}

		protected event PropertyChangeHandler<ArraySchema<float>> _arrayOfNumbersChange;
		public Action OnArrayOfNumbersChange(PropertyChangeHandler<ArraySchema<float>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(arrayOfNumbers));
			_arrayOfNumbersChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(arrayOfNumbers));
				_arrayOfNumbersChange -= handler;
			};
		}

		protected event PropertyChangeHandler<ArraySchema<string>> _arrayOfStringsChange;
		public Action OnArrayOfStringsChange(PropertyChangeHandler<ArraySchema<string>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(arrayOfStrings));
			_arrayOfStringsChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(arrayOfStrings));
				_arrayOfStringsChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(num): _numChange?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(str): _strChange?.Invoke((string) change.Value, (string) change.PreviousValue); break;
				case nameof(aRef): _aRefChange?.Invoke((Ref) change.Value, (Ref) change.PreviousValue); break;
				case nameof(arrayOfSchemas): _arrayOfSchemasChange?.Invoke((ArraySchema<Ref>) change.Value, (ArraySchema<Ref>) change.PreviousValue); break;
				case nameof(arrayOfNumbers): _arrayOfNumbersChange?.Invoke((ArraySchema<float>) change.Value, (ArraySchema<float>) change.PreviousValue); break;
				case nameof(arrayOfStrings): _arrayOfStringsChange?.Invoke((ArraySchema<string>) change.Value, (ArraySchema<string>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

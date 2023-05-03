// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.ArraySchemaTypes {
	public partial class ArraySchemaTypes : Schema {
		[Type(0, "array", typeof(ArraySchema<IAmAChild>))]
		public ArraySchema<IAmAChild> arrayOfSchemas = new ArraySchema<IAmAChild>();

		[Type(1, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> arrayOfNumbers = new ArraySchema<float>();

		[Type(2, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();

		[Type(3, "array", typeof(ArraySchema<int>), "int32")]
		public ArraySchema<int> arrayOfInt32 = new ArraySchema<int>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<ArraySchema<IAmAChild>> _arrayOfSchemasChange;
		public Action OnArrayOfSchemasChange(PropertyChangeHandler<ArraySchema<IAmAChild>> handler) {
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

		protected event PropertyChangeHandler<ArraySchema<int>> _arrayOfInt32Change;
		public Action OnArrayOfInt32Change(PropertyChangeHandler<ArraySchema<int>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(arrayOfInt32));
			_arrayOfInt32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(arrayOfInt32));
				_arrayOfInt32Change -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(arrayOfSchemas): _arrayOfSchemasChange?.Invoke((ArraySchema<IAmAChild>) change.Value, (ArraySchema<IAmAChild>) change.PreviousValue); break;
				case nameof(arrayOfNumbers): _arrayOfNumbersChange?.Invoke((ArraySchema<float>) change.Value, (ArraySchema<float>) change.PreviousValue); break;
				case nameof(arrayOfStrings): _arrayOfStringsChange?.Invoke((ArraySchema<string>) change.Value, (ArraySchema<string>) change.PreviousValue); break;
				case nameof(arrayOfInt32): _arrayOfInt32Change?.Invoke((ArraySchema<int>) change.Value, (ArraySchema<int>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

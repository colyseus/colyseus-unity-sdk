// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.BackwardsForwards {
	public partial class PlayerV2 : Schema {
		[Type(0, "number")]
		public float x = default(float);

		[Type(1, "number")]
		public float y = default(float);

		[Type(2, "string")]
		public string name = default(string);

		[Type(3, "array", typeof(ArraySchema<string>), "string")]
		public ArraySchema<string> arrayOfStrings = new ArraySchema<string>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<float> _xChange;
		public Action OnXChange(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(x));
			_xChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(x));
				_xChange -= handler;
			};
		}

		protected event PropertyChangeHandler<float> _yChange;
		public Action OnYChange(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(y));
			_yChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(y));
				_yChange -= handler;
			};
		}

		protected event PropertyChangeHandler<string> _nameChange;
		public Action OnNameChange(PropertyChangeHandler<string> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(name));
			_nameChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(name));
				_nameChange -= handler;
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
				case nameof(x): _xChange?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(y): _yChange?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(name): _nameChange?.Invoke((string) change.Value, (string) change.PreviousValue); break;
				case nameof(arrayOfStrings): _arrayOfStringsChange?.Invoke((ArraySchema<string>) change.Value, (ArraySchema<string>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

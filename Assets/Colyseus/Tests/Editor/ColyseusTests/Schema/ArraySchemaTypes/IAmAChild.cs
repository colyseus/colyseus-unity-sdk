// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.ArraySchemaTypes {
	public partial class IAmAChild : Schema {
		[Type(0, "number")]
		public float x = default(float);

		[Type(1, "number")]
		public float y = default(float);

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

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(x): _xChange?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				case nameof(y): _yChange?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

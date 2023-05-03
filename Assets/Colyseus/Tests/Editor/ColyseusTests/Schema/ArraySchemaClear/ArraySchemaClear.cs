// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.ArraySchemaClear {
	public partial class ArraySchemaClear : Schema {
		[Type(0, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> items = new ArraySchema<float>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<ArraySchema<float>> _itemsChange;
		public Action OnItemsChange(PropertyChangeHandler<ArraySchema<float>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(items));
			_itemsChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(items));
				_itemsChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(items): _itemsChange?.Invoke((ArraySchema<float>) change.Value, (ArraySchema<float>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

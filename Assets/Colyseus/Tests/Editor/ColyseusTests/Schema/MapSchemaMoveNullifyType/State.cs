// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.MapSchemaMoveNullifyType {
	public partial class State : Schema {
		[Type(0, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> previous = new MapSchema<float>();

		[Type(1, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> current = new MapSchema<float>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<MapSchema<float>> _previousChange;
		public Action OnPreviousChange(PropertyChangeHandler<MapSchema<float>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(previous));
			_previousChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(previous));
				_previousChange -= handler;
			};
		}

		protected event PropertyChangeHandler<MapSchema<float>> _currentChange;
		public Action OnCurrentChange(PropertyChangeHandler<MapSchema<float>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(current));
			_currentChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(current));
				_currentChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(previous): _previousChange?.Invoke((MapSchema<float>) change.Value, (MapSchema<float>) change.PreviousValue); break;
				case nameof(current): _currentChange?.Invoke((MapSchema<float>) change.Value, (MapSchema<float>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

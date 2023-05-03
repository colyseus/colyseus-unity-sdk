// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.MapSchemaInt8 {
	public partial class MapSchemaInt8 : Schema {
		[Type(0, "string")]
		public string status = default(string);

		[Type(1, "map", typeof(MapSchema<sbyte>), "int8")]
		public MapSchema<sbyte> mapOfInt8 = new MapSchema<sbyte>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<string> _statusChange;
		public Action OnStatusChange(PropertyChangeHandler<string> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(status));
			_statusChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(status));
				_statusChange -= handler;
			};
		}

		protected event PropertyChangeHandler<MapSchema<sbyte>> _mapOfInt8Change;
		public Action OnMapOfInt8Change(PropertyChangeHandler<MapSchema<sbyte>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(mapOfInt8));
			_mapOfInt8Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(mapOfInt8));
				_mapOfInt8Change -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(status): _statusChange?.Invoke((string) change.Value, (string) change.PreviousValue); break;
				case nameof(mapOfInt8): _mapOfInt8Change?.Invoke((MapSchema<sbyte>) change.Value, (MapSchema<sbyte>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

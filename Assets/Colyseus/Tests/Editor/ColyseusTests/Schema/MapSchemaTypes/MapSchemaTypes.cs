// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.MapSchemaTypes {
	public partial class MapSchemaTypes : Schema {
		[Type(0, "map", typeof(MapSchema<IAmAChild>))]
		public MapSchema<IAmAChild> mapOfSchemas = new MapSchema<IAmAChild>();

		[Type(1, "map", typeof(MapSchema<float>), "number")]
		public MapSchema<float> mapOfNumbers = new MapSchema<float>();

		[Type(2, "map", typeof(MapSchema<string>), "string")]
		public MapSchema<string> mapOfStrings = new MapSchema<string>();

		[Type(3, "map", typeof(MapSchema<int>), "int32")]
		public MapSchema<int> mapOfInt32 = new MapSchema<int>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<MapSchema<IAmAChild>> _mapOfSchemasChange;
		public Action OnMapOfSchemasChange(PropertyChangeHandler<MapSchema<IAmAChild>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(mapOfSchemas));
			_mapOfSchemasChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(mapOfSchemas));
				_mapOfSchemasChange -= handler;
			};
		}

		protected event PropertyChangeHandler<MapSchema<float>> _mapOfNumbersChange;
		public Action OnMapOfNumbersChange(PropertyChangeHandler<MapSchema<float>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(mapOfNumbers));
			_mapOfNumbersChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(mapOfNumbers));
				_mapOfNumbersChange -= handler;
			};
		}

		protected event PropertyChangeHandler<MapSchema<string>> _mapOfStringsChange;
		public Action OnMapOfStringsChange(PropertyChangeHandler<MapSchema<string>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(mapOfStrings));
			_mapOfStringsChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(mapOfStrings));
				_mapOfStringsChange -= handler;
			};
		}

		protected event PropertyChangeHandler<MapSchema<int>> _mapOfInt32Change;
		public Action OnMapOfInt32Change(PropertyChangeHandler<MapSchema<int>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(mapOfInt32));
			_mapOfInt32Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(mapOfInt32));
				_mapOfInt32Change -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(mapOfSchemas): _mapOfSchemasChange?.Invoke((MapSchema<IAmAChild>) change.Value, (MapSchema<IAmAChild>) change.PreviousValue); break;
				case nameof(mapOfNumbers): _mapOfNumbersChange?.Invoke((MapSchema<float>) change.Value, (MapSchema<float>) change.PreviousValue); break;
				case nameof(mapOfStrings): _mapOfStringsChange?.Invoke((MapSchema<string>) change.Value, (MapSchema<string>) change.PreviousValue); break;
				case nameof(mapOfInt32): _mapOfInt32Change?.Invoke((MapSchema<int>) change.Value, (MapSchema<int>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

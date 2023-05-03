// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.BackwardsForwards {
	public partial class StateV1 : Schema {
		[Type(0, "string")]
		public string str = default(string);

		[Type(1, "map", typeof(MapSchema<PlayerV1>))]
		public MapSchema<PlayerV1> map = new MapSchema<PlayerV1>();

		/*
		 * Support for individual property change callbacks below...
		 */

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

		protected event PropertyChangeHandler<MapSchema<PlayerV1>> _mapChange;
		public Action OnMapChange(PropertyChangeHandler<MapSchema<PlayerV1>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(map));
			_mapChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(map));
				_mapChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(str): _strChange?.Invoke((string) change.Value, (string) change.PreviousValue); break;
				case nameof(map): _mapChange?.Invoke((MapSchema<PlayerV1>) change.Value, (MapSchema<PlayerV1>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

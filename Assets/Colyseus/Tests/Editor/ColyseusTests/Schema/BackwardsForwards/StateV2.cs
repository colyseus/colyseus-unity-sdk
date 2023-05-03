// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.BackwardsForwards {
	public partial class StateV2 : Schema {
		[Type(0, "string")]
		public string str = default(string);

		[System.Obsolete("field 'map' is deprecated.", true)]
		[Type(1, "map", typeof(MapSchema<PlayerV2>))]
		public MapSchema<PlayerV2> map = new MapSchema<PlayerV2>();

		[Type(2, "number")]
		public float countdown = default(float);

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

		protected event PropertyChangeHandler<float> _countdownChange;
		public Action OnCountdownChange(PropertyChangeHandler<float> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(countdown));
			_countdownChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(countdown));
				_countdownChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(str): _strChange?.Invoke((string) change.Value, (string) change.PreviousValue); break;
				case nameof(countdown): _countdownChange?.Invoke((float) change.Value, (float) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

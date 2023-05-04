// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.InstanceSharingTypes {
	public partial class Player : Schema {
		[Type(0, "ref", typeof(Position))]
		public Position position = new Position();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<Position> _positionChange;
		public Action OnPositionChange(PropertyChangeHandler<Position> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(position));
			_positionChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(position));
				_positionChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(position): _positionChange?.Invoke((Position) change.Value, (Position) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

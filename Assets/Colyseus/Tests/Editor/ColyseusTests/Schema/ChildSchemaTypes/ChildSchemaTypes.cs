// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.ChildSchemaTypes {
	public partial class ChildSchemaTypes : Schema {
		[Type(0, "ref", typeof(IAmAChild))]
		public IAmAChild child = new IAmAChild();

		[Type(1, "ref", typeof(IAmAChild))]
		public IAmAChild secondChild = new IAmAChild();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<IAmAChild> _childChange;
		public Action OnChildChange(PropertyChangeHandler<IAmAChild> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(child));
			_childChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(child));
				_childChange -= handler;
			};
		}

		protected event PropertyChangeHandler<IAmAChild> _secondChildChange;
		public Action OnSecondChildChange(PropertyChangeHandler<IAmAChild> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(secondChild));
			_secondChildChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(secondChild));
				_secondChildChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(child): _childChange?.Invoke((IAmAChild) change.Value, (IAmAChild) change.PreviousValue); break;
				case nameof(secondChild): _secondChildChange?.Invoke((IAmAChild) change.Value, (IAmAChild) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.Callbacks {
	public partial class CallbacksState : Schema {
		[Type(0, "ref", typeof(Container))]
		public Container container = new Container();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<Container> _containerChange;
		public Action OnContainerChange(PropertyChangeHandler<Container> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(container));
			_containerChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(container));
				_containerChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(container): _containerChange?.Invoke((Container) change.Value, (Container) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

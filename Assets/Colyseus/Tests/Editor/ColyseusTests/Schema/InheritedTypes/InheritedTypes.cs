// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.InheritedTypes {
	public partial class InheritedTypes : Schema {
		[Type(0, "ref", typeof(Entity))]
		public Entity entity = new Entity();

		[Type(1, "ref", typeof(Player))]
		public Player player = new Player();

		[Type(2, "ref", typeof(Bot))]
		public Bot bot = new Bot();

		[Type(3, "ref", typeof(Entity))]
		public Entity any = new Entity();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<Entity> _entityChange;
		public Action OnEntityChange(PropertyChangeHandler<Entity> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(entity));
			_entityChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(entity));
				_entityChange -= handler;
			};
		}

		protected event PropertyChangeHandler<Player> _playerChange;
		public Action OnPlayerChange(PropertyChangeHandler<Player> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(player));
			_playerChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(player));
				_playerChange -= handler;
			};
		}

		protected event PropertyChangeHandler<Bot> _botChange;
		public Action OnBotChange(PropertyChangeHandler<Bot> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(bot));
			_botChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(bot));
				_botChange -= handler;
			};
		}

		protected event PropertyChangeHandler<Entity> _anyChange;
		public Action OnAnyChange(PropertyChangeHandler<Entity> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(any));
			_anyChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(any));
				_anyChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(entity): _entityChange?.Invoke((Entity) change.Value, (Entity) change.PreviousValue); break;
				case nameof(player): _playerChange?.Invoke((Player) change.Value, (Player) change.PreviousValue); break;
				case nameof(bot): _botChange?.Invoke((Bot) change.Value, (Bot) change.PreviousValue); break;
				case nameof(any): _anyChange?.Invoke((Entity) change.Value, (Entity) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

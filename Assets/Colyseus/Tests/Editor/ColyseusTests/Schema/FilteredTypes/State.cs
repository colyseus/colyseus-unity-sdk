// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.FilteredTypes {
	public partial class State : Schema {
		[Type(0, "ref", typeof(Player))]
		public Player playerOne = new Player();

		[Type(1, "ref", typeof(Player))]
		public Player playerTwo = new Player();

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> players = new ArraySchema<Player>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<Player> _playerOneChange;
		public Action OnPlayerOneChange(PropertyChangeHandler<Player> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(playerOne));
			_playerOneChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(playerOne));
				_playerOneChange -= handler;
			};
		}

		protected event PropertyChangeHandler<Player> _playerTwoChange;
		public Action OnPlayerTwoChange(PropertyChangeHandler<Player> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(playerTwo));
			_playerTwoChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(playerTwo));
				_playerTwoChange -= handler;
			};
		}

		protected event PropertyChangeHandler<ArraySchema<Player>> _playersChange;
		public Action OnPlayersChange(PropertyChangeHandler<ArraySchema<Player>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(players));
			_playersChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(players));
				_playersChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(playerOne): _playerOneChange?.Invoke((Player) change.Value, (Player) change.PreviousValue); break;
				case nameof(playerTwo): _playerTwoChange?.Invoke((Player) change.Value, (Player) change.PreviousValue); break;
				case nameof(players): _playersChange?.Invoke((ArraySchema<Player>) change.Value, (ArraySchema<Player>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

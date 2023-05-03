// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 2.0.5
// 

using Colyseus.Schema;
using Action = System.Action;

namespace SchemaTest.InstanceSharingTypes {
	public partial class State : Schema {
		[Type(0, "ref", typeof(Player))]
		public Player player1 = new Player();

		[Type(1, "ref", typeof(Player))]
		public Player player2 = new Player();

		[Type(2, "array", typeof(ArraySchema<Player>))]
		public ArraySchema<Player> arrayOfPlayers = new ArraySchema<Player>();

		[Type(3, "map", typeof(MapSchema<Player>))]
		public MapSchema<Player> mapOfPlayers = new MapSchema<Player>();

		/*
		 * Support for individual property change callbacks below...
		 */

		protected event PropertyChangeHandler<Player> _player1Change;
		public Action OnPlayer1Change(PropertyChangeHandler<Player> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(player1));
			_player1Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(player1));
				_player1Change -= handler;
			};
		}

		protected event PropertyChangeHandler<Player> _player2Change;
		public Action OnPlayer2Change(PropertyChangeHandler<Player> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(player2));
			_player2Change += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(player2));
				_player2Change -= handler;
			};
		}

		protected event PropertyChangeHandler<ArraySchema<Player>> _arrayOfPlayersChange;
		public Action OnArrayOfPlayersChange(PropertyChangeHandler<ArraySchema<Player>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(arrayOfPlayers));
			_arrayOfPlayersChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(arrayOfPlayers));
				_arrayOfPlayersChange -= handler;
			};
		}

		protected event PropertyChangeHandler<MapSchema<Player>> _mapOfPlayersChange;
		public Action OnMapOfPlayersChange(PropertyChangeHandler<MapSchema<Player>> handler) {
			if (__callbacks == null) { __callbacks = new SchemaCallbacks(); }
			__callbacks.AddPropertyCallback(nameof(mapOfPlayers));
			_mapOfPlayersChange += handler;
			return () => {
				__callbacks.RemovePropertyCallback(nameof(mapOfPlayers));
				_mapOfPlayersChange -= handler;
			};
		}

		protected override void TriggerFieldChange(DataChange change) {
			switch (change.Field) {
				case nameof(player1): _player1Change?.Invoke((Player) change.Value, (Player) change.PreviousValue); break;
				case nameof(player2): _player2Change?.Invoke((Player) change.Value, (Player) change.PreviousValue); break;
				case nameof(arrayOfPlayers): _arrayOfPlayersChange?.Invoke((ArraySchema<Player>) change.Value, (ArraySchema<Player>) change.PreviousValue); break;
				case nameof(mapOfPlayers): _mapOfPlayersChange?.Invoke((MapSchema<Player>) change.Value, (MapSchema<Player>) change.PreviousValue); break;
				default: break;
			}
		}
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Colyseus;

using GameDevWare.Serialization;
using GameDevWare.Serialization.MessagePack;

public class ColyseusClient : MonoBehaviour {

	Client client;
	Room room;
	public string serverName = "localhost";
	public string port = "3553";
	public string roomName = "chat";

	// map of players
	Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

	// Use this for initialization
	IEnumerator Start () {

		String uri = "ws://" + serverName + ":" + port;
		client = new Client(uri);
		client.OnOpen += OnOpenHandler;
		client.OnClose += (object sender, EventArgs e) => Debug.Log ("CONNECTION CLOSED");

		yield return StartCoroutine(client.Connect());

		room = client.Join(roomName);
		room.OnReadyToConnect += (sender, e) => StartCoroutine ( room.Connect() );
		room.OnJoin += OnRoomJoined;
		room.OnUpdate += OnUpdateHandler;

		room.Listen ("players/:id", this.OnPlayerChange);
		room.Listen ("players/:id/:axis", this.OnPlayerMove);
		room.Listen ("messages/:number", this.OnMessageAdded);
		room.Listen (this.OnChangeFallback);

		room.OnData += (object sender, MessageEventArgs e) => Debug.Log(e.data);

		int i = 0;

		while (true)
		{
			client.Recv();

			// string reply = client.RecvString();
			if (client.error != null)
			{
				Debug.LogError ("Error: " + client.error);
				break;
			}

			i++;

			if (i % 50 == 0) {
				room.Send("some_command");
			}

			yield return 0;
		}

		OnApplicationQuit();
	}

	void OnDestroy ()
	{
		// Make sure client will disconnect from the server
		room.Leave ();
		client.Close ();
	}

	void OnOpenHandler (object sender, EventArgs e)
	{
		Debug.Log("Connected to server. Client id: " + client.id);
	}

	void OnRoomJoined (object sender, EventArgs e)
	{
		Debug.Log("Joined room successfully.");
	}

	void OnUpdateHandler (object sender, RoomUpdateEventArgs e)
	{
		// Setup room first state
		if (e.isFirstState) {
			IndexedDictionary<string, object> players = (IndexedDictionary<string, object>) e.state ["players"];

			// trigger to add existing players 
			foreach(KeyValuePair<string, object> player in players)
			{
				this.OnPlayerChange (new DataChange {
					path = new Dictionary<string, string> {
						{"id", player.Key}
					},
					operation = "add",
					value = player.Value
				});
			}
		}
	}

	void OnPlayerChange (DataChange change)
	{
		Debug.Log ("OnPlayerChange");
		Debug.Log (change.operation);
		Debug.Log (change.path["id"]);
		Debug.Log (change.value);

		if (change.operation == "add") {
			IndexedDictionary<string, object> value = (IndexedDictionary<string, object>) change.value;

			GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);

			cube.transform.position = new Vector3 (Convert.ToSingle(value ["x"]), Convert.ToSingle(value ["y"]), 0);

			// add "player" to map of players by id.
			players.Add (change.path ["id"], cube);

		} else if (change.operation == "remove") {
			// remove player from scene
			GameObject cube;
			players.TryGetValue (change.path ["id"], out cube);
			Destroy (cube);

			players.Remove (change.path ["id"]);
		}
	}

	void OnPlayerMove (DataChange change)
	{
		Debug.Log ("OnPlayerMove");
		Debug.Log ("playerId: " + change.path["id"] + ", Axis: " + change.path["axis"]);
		Debug.Log (change.value);

		GameObject cube;
		players.TryGetValue (change.path ["id"], out cube);

		cube.transform.Translate (new Vector3 (Convert.ToSingle(change.value), 0, 0));
	}

	void OnPlayerRemoved (DataChange change)
	{
		Debug.Log ("OnPlayerRemoved");
		Debug.Log (change.path);
		Debug.Log (change.value);
	}

	void OnMessageAdded (DataChange change)
	{
		Debug.Log ("OnMessageAdded");
		Debug.Log (change.path["number"]);
		Debug.Log (change.value);
	}

	void OnChangeFallback (PatchObject change)
	{
		// Debug.Log ("OnChangeFallback");
		// Debug.Log (change.operation);
		// Debug.Log (change.path);
		// Debug.Log (change.value);
	}

	void OnApplicationQuit()
	{
		// Ensure the connection with server is closed immediatelly
		client.Close();
	}
}

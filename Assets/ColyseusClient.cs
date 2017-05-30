using UnityEngine;
using System.Collections;
using System;
using Colyseus;
using GameDevWare.Serialization;

public class ColyseusClient : MonoBehaviour
{
	Client colyseus;
	Room chatRoom;
	public string serverName = "localhost";
	public string port = "3553";
	public string roomName = "chat";

	// Use this for initialization
	IEnumerator Start()
	{
		String uri = "ws://" + serverName + ":" + port;
		colyseus = new Client(uri);
		colyseus.OnOpen += OnOpenHandler;
		yield return StartCoroutine(colyseus.Connect());

		chatRoom = colyseus.Join(roomName);
		chatRoom.OnJoin += OnRoomJoined;
		chatRoom.OnUpdate += OnUpdateHandler;

		chatRoom.state.Listen("players", "add", this.OnAddPlayer);
		chatRoom.state.Listen("players/:id/:axis", "replace", this.OnPlayerMove);
		chatRoom.state.Listen("players/:id", "remove", this.OnPlayerRemoved);
		chatRoom.state.Listen(this.OnChangeFallback);

		int i = 0;

		while (true)
		{
			colyseus.Recv();

			// string reply = colyseus.RecvString();
			if (colyseus.error != null)
			{
				Debug.LogError("Error: " + colyseus.error);
				break;
			}

			i++;

			if (i % 50 == 0)
			{
				chatRoom.Send("some_command");
			}
			yield return 0;
		}

		OnApplicationQuit();
	}

	void OnOpenHandler(object sender, EventArgs e)
	{
		Debug.Log("Connected to server. Client id: " + colyseus.id);
	}

	void OnRoomJoined(object sender, EventArgs e)
	{
		Debug.Log("Joined room successfully.");
	}

	void OnAddPlayer(string[] path, object value)
	{
		Debug.Log("OnAddPlayer");
		Debug.Log(path[0]);
		Debug.Log(value);
	}

	void OnPlayerMove(string[] path, object value)
	{
		Debug.Log("OnPlayerMove");
		Debug.Log(path[0]);
		Debug.Log(value);
	}

	void OnPlayerRemoved(string[] path, object value)
	{
		Debug.Log("OnPlayerRemoved");
		Debug.Log(value);
	}

	void OnChangeFallback(string[] path, string operation, object value)
	{
		Debug.Log("OnChangeFallback");
		Debug.Log(operation);
		Debug.Log(path[0]);
		Debug.Log(value);
	}

	void OnUpdateHandler(object sender, RoomUpdateEventArgs e)
	{
		//Debug.Log(e.state);
	}

	void OnApplicationQuit()
	{
		// Ensure the connection with server is closed immediatelly
		colyseus.Close();
	}
}
using UnityEngine;
using System.Collections;
using System;
using Colyseus;

public class ColyseusClient : MonoBehaviour {

	Client colyseus;
	Room chatRoom;

	// Use this for initialization
	IEnumerator Start () {
		colyseus = new Client("ws://localhost:3553");
		colyseus.OnOpen += OnOpenHandler;
		yield return StartCoroutine(colyseus.Connect());

		chatRoom = colyseus.Join("chat");
		chatRoom.OnJoin += OnRoomJoined;
		chatRoom.OnUpdate += OnUpdateHandler;

		int i = 0;

		while (true)
		{
			colyseus.Recv();

			// string reply = colyseus.RecvString();
			if (colyseus.error != null)
			{
				Debug.LogError ("Error: "+colyseus.error);
				break;
			}

			i++;

			if (i % 50 == 0) {
				chatRoom.Send("Hey!");
			}

			yield return 0;
		}

		OnApplicationQuit();
	}

	void OnRoomJoined (object sender, EventArgs e)
	{
		Debug.Log("Joined room");
	}

	void OnOpenHandler (object sender, EventArgs e)
	{
		Debug.Log(colyseus.id);
	}

	void OnUpdateHandler (object sender, RoomUpdateEventArgs e)
	{
		Debug.Log(e.state);
	}

	void OnApplicationQuit()
	{
		// Ensure the connection with server is closed immediatelly
		colyseus.Close();
	}

}

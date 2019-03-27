using UnityEngine;
using System.Collections;

using Colyseus;
using GameDevWare.Serialization;

public class ClientComponent : MonoBehaviour
{
	public Client client;
	public Room<IndexedDictionary<string, object>> room;

	// Use this for initialization
	public IEnumerator Start () {
		client = new Client("ws://localhost:2567");

		yield return StartCoroutine(client.Connect());

		room = client.Join("chat");
		room.OnReadyToConnect += (sender, e) => StartCoroutine ( room.Connect() );

		while (true)
		{
			client.Recv();
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

	void OnApplicationQuit()
	{
		// Ensure the connection with server is closed immediatelly
		OnDestroy();
	}
}

using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

using Colyseus;
using GameDevWare.Serialization;

public class ClientComponent : MonoBehaviour
{
	public Client client;
	public Room<IndexedDictionary<string, object>> room;

	// Use this for initialization
	public async void Start () {
		client = new Client("ws://localhost:2567");

		await client.Connect();

		room = await client.Join("chat");
		await room.Connect();

		// OnApplicationQuit();
	}

	async void OnDestroy ()
	{
		// Make sure client will disconnect from the server
		await room.Leave ();
		await client.Close ();
	}

	void OnApplicationQuit()
	{
		// Ensure the connection with server is closed immediatelly
		OnDestroy();
	}
}

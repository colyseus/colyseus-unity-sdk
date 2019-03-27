using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;
using System;
using Colyseus;
using Colyseus.Schema;

using GameDevWare.Serialization;

public class ColyseusClient : MonoBehaviour {

	// UI Buttons are attached through Unity Inspector
	public Button m_ConnectButton, m_JoinButton, m_ReJoinButton, m_SendMessageButton, m_LeaveButton;
	public InputField m_EndpointField;
	public Text m_IdText, m_SessionIdText;

	public string roomName = "demo";

	protected Client client;
	protected Room<State> room;

	protected IndexedDictionary<Player, GameObject> players = new IndexedDictionary<Player, GameObject>();

	// Use this for initialization
	IEnumerator Start () {
		/* Demo UI */
		m_ConnectButton.onClick.AddListener(ConnectToServer);

		m_JoinButton.onClick.AddListener(JoinRoom);
		m_ReJoinButton.onClick.AddListener(ReJoinRoom);
		m_SendMessageButton.onClick.AddListener(SendMessage);
		m_LeaveButton.onClick.AddListener(LeaveRoom);

		/* Always call Recv if Colyseus connection is open */
		while (true)
		{
			if (client != null)
			{
				client.Recv();
			}
			yield return 0;
		}
	}

	void ConnectToServer ()
	{
		/*
		 * Get Colyseus endpoint from InputField
		 */
		string endpoint = m_EndpointField.text;

		Debug.Log("Connecting to " + endpoint);

		/*
		 * Connect into Colyeus Server
		 */
		client = new Client(endpoint);
		client.OnOpen += (object sender, EventArgs e) => {
			/* Update Demo UI */
			m_IdText.text = "id: " + client.Id;
		};
		client.OnError += (sender, e) => Debug.LogError(e.Message);
		client.OnClose += (sender, e) => Debug.Log("CONNECTION CLOSED");
		StartCoroutine(client.Connect());
	}

	void JoinRoom ()
	{
		room = client.Join<State>(roomName, new Dictionary<string, object>()
		{
			{ "create", true }
		});

		room.OnReadyToConnect += (sender, e) => {
			Debug.Log("Ready to connect to room!");
			StartCoroutine(room.Connect());
		};
		room.OnError += (sender, e) =>
		{
			Debug.LogError(e.Message);
		};
		room.OnJoin += (sender, e) => {
			Debug.Log("Joined room successfully.");
			m_SessionIdText.text = "sessionId: " + room.SessionId;

			room.State.players.OnAdd += OnPlayerAdd;
			room.State.players.OnRemove += OnPlayerRemove;
			room.State.players.OnChange += OnPlayerMove;

			PlayerPrefs.SetString("sessionId", room.SessionId);
			PlayerPrefs.Save();
		};

		room.OnStateChange += OnStateChangeHandler;
		room.OnMessage += OnMessage;
	}

	void ReJoinRoom ()
	{
		string sessionId = PlayerPrefs.GetString("sessionId");
		if (string.IsNullOrEmpty(sessionId))
		{
			Debug.Log("Cannot ReJoin without having a sessionId");
			return;
		}

		room = client.ReJoin<State>(roomName, sessionId);

		room.OnReadyToConnect += (sender, e) => {
			Debug.Log("Ready to connect to room!");
			StartCoroutine(room.Connect());
		};
		room.OnError += (sender, e) => Debug.LogError(e.Message);
		room.OnJoin += (sender, e) => {
			Debug.Log("Joined room successfully.");
			m_SessionIdText.text = "sessionId: " + room.SessionId;

			room.State.players.OnAdd += OnPlayerAdd;
			room.State.players.OnRemove += OnPlayerRemove;
			room.State.players.OnChange += OnPlayerMove;
		};

		room.OnStateChange += OnStateChangeHandler;
		room.OnMessage += OnMessage;
	}

	void LeaveRoom()
	{
		room.Leave(false);

		// Destroy player entities
		foreach (KeyValuePair<Player, GameObject> entry in players)
		{
			Destroy(entry.Value);
		}

		players.Clear();
	}

	void SendMessage()
	{
		if (room != null)
		{
			room.Send("move_right");
		}
		else
		{
			Debug.Log("Room is not connected!");
		}
	}

	void OnMessage (object sender, MessageEventArgs e)
	{
		var message = (IndexedDictionary<string, object>) e.Message;
		Debug.Log(message);
	}

	void OnStateChangeHandler (object sender, StateChangeEventArgs<State> e)
	{
		// Setup room first state
		Debug.Log("State has been updated!");
	}

	void OnPlayerAdd(object sender, KeyValueEventArgs<Player, string> item)
	{
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

		Debug.Log("Player add! x => " + item.Value.x + ", y => " + item.Value.y);

		cube.transform.position = new Vector3(item.Value.x, item.Value.y, 0);

		// add "player" to map of players
		players.Add(item.Value, cube);
	}

	void OnPlayerRemove(object sender, KeyValueEventArgs<Player, string> item)
	{
		GameObject cube;
		players.TryGetValue(item.Value, out cube);
		Destroy(cube);

		players.Remove(item.Value);
	}


	void OnPlayerMove (object sender, KeyValueEventArgs<Player, string> item)
	{
		GameObject cube;
		players.TryGetValue (item.Value, out cube);

		Debug.Log(item.Value.x);

		cube.transform.Translate (new Vector3 (item.Value.x, item.Value.y, 0));
	}

	void OnApplicationQuit()
	{
		// Make sure client will disconnect from the server
		if (room != null)
		{
			room.Leave();
		}

		if (client != null)
		{
			client.Close();
		}
	}
}

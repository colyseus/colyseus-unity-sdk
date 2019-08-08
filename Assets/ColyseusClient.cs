using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

using Colyseus;
using Colyseus.Schema;

using GameDevWare.Serialization;

public class ColyseusClient : MonoBehaviour {

	// UI Buttons are attached through Unity Inspector
	public Button m_ConnectButton, m_JoinButton, m_ReJoinButton, m_SendMessageButton, m_LeaveButton, m_GetAvailableRoomsButton;
	public InputField m_EndpointField;
	public Text m_IdText, m_SessionIdText;

	public string roomName = "demo";

	protected Client client;
	protected Room<State> room;

	protected IndexedDictionary<Entity, GameObject> entities = new IndexedDictionary<Entity, GameObject>();

	// Use this for initialization
	void Start () {
		/* Demo UI */
		m_ConnectButton.onClick.AddListener(ConnectToServer);

		m_JoinButton.onClick.AddListener(JoinRoom);
		m_ReJoinButton.onClick.AddListener(ReJoinRoom);
		m_SendMessageButton.onClick.AddListener(SendMessage);
		m_LeaveButton.onClick.AddListener(LeaveRoom);
		m_GetAvailableRoomsButton.onClick.AddListener(GetAvailableRooms);
	}

	async void ConnectToServer ()
	{
		/*
		 * Get Colyseus endpoint from InputField
		 */
		string endpoint = m_EndpointField.text;

		Debug.Log("Connecting to " + endpoint);

		/*
		 * Connect into Colyeus Server
		 */
		client = ColyseusManager.Instance.CreateClient(endpoint);

		await client.Auth.Login();

		var friends = await client.Auth.GetFriends();

		// Update username
		client.Auth.Username = "Jake";
		await client.Auth.Save();

		client.OnOpen += () => {
			/* Update Demo UI */
			m_IdText.text = "id: " + client.Id;
		};
		client.OnError += (message) => Debug.LogError(message);
		client.OnClose += (code) => Debug.Log("CONNECTION CLOSED");

		await client.Connect();
	}

	public async void JoinRoom ()
	{
		room = await client.Join<State>(roomName, new Dictionary<string, object>()
		{
			{ "create", true }
		});

		room.OnLeave += (code) => Debug.Log("ROOM: ON LEAVE");
		room.OnError += (message) => Debug.LogError(message);

		room.OnJoin += () => {
			m_SessionIdText.text = "sessionId: " + room.SessionId;

			room.State.entities.OnAdd += OnEntityAdd;
			room.State.entities.OnRemove += OnEntityRemove;
			room.State.entities.OnChange += OnEntityMove;

			PlayerPrefs.SetString("sessionId", room.SessionId);
			PlayerPrefs.Save();
		};

		room.OnStateChange += OnStateChangeHandler;
		room.OnMessage += OnMessage;
	}

	async void ReJoinRoom ()
	{
		string sessionId = PlayerPrefs.GetString("sessionId");
		if (string.IsNullOrEmpty(sessionId))
		{
			Debug.Log("Cannot ReJoin without having a sessionId");
			return;
		}

		room = await client.ReJoin<State>(roomName, sessionId);

		room.OnError += (message) => Debug.LogError(message);
		room.OnJoin += () => {
			Debug.Log("Joined room successfully.");
			m_SessionIdText.text = "sessionId: " + room.SessionId;

			room.State.entities.OnAdd += OnEntityAdd;
			room.State.entities.OnRemove += OnEntityRemove;
			room.State.entities.OnChange += OnEntityMove;
		};

		room.OnStateChange += OnStateChangeHandler;
		room.OnMessage += OnMessage;
	}

	async void LeaveRoom()
	{
		await room.Leave(false);

		// Destroy player entities
		foreach (KeyValuePair<Entity, GameObject> entry in entities)
		{
			Destroy(entry.Value);
		}

		entities.Clear();
	}

	async void GetAvailableRooms()
	{
		await client.GetAvailableRooms(roomName, (RoomAvailable[] roomsAvailable) =>
		{
			Debug.Log("Available rooms (" + roomsAvailable.Length + ")");
			for (var i=0; i< roomsAvailable.Length;i++)
			{
				Debug.Log("roomId: " + roomsAvailable[i].roomId);
				Debug.Log("maxClients: " + roomsAvailable[i].maxClients);
				Debug.Log("clients: " + roomsAvailable[i].clients);
				Debug.Log("metadata: " + roomsAvailable[i].metadata);
			}
		});
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

	void OnMessage (object msg)
	{
		var message = (IndexedDictionary<string, object>)msg;
		Debug.Log(message);
	}

	void OnStateChangeHandler (State state, bool isFirstState)
	{
		// Setup room first state
		Debug.Log("State has been updated!");
	}

	void OnEntityAdd(Entity entity, string key)
	{
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

		Debug.Log("Player add! x => " + entity.x + ", y => " + entity.y);

		cube.transform.position = new Vector3(entity.x, entity.y, 0);

		// add "player" to map of players
		entities.Add(entity, cube);
	}

	void OnEntityRemove(Entity entity, string key)
	{
		GameObject cube;
		entities.TryGetValue(entity, out cube);
		Destroy(cube);

		entities.Remove(entity);
	}


	void OnEntityMove(Entity entity, string key)
	{
		GameObject cube;
		entities.TryGetValue (entity, out cube);

		Debug.Log(entity);

		cube.transform.Translate (new Vector3 (entity.x, entity.y, 0));
	}

	void OnApplicationQuit()
	{
	}
}

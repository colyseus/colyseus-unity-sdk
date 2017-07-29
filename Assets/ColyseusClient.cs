using UnityEngine;
using System.Collections;
using System;
using Colyseus;
using MsgPack;

public class ColyseusClient : MonoBehaviour {

    Client colyseus;
    Room chatRoom;
    public string serverName = "localhost";
    public string port = "3553";
    public string roomName = "chat";

    // Use this for initialization
    IEnumerator Start () {
        String uri = "ws://" + serverName + ":" + port;
        colyseus = new Client(uri);
        colyseus.OnOpen += OnOpenHandler;

        yield return StartCoroutine(colyseus.Connect());

        chatRoom = colyseus.Join(roomName);
        chatRoom.OnReadyToConnect += (sender, e) => StartCoroutine ( chatRoom.Connect() );
        chatRoom.OnJoin += OnRoomJoined;
        chatRoom.OnUpdate += OnUpdateHandler;

        chatRoom.Listen ("players", this.OnAddPlayer);
        chatRoom.Listen ("players/:id/:axis", this.OnPlayerMove);
        chatRoom.Listen ("players/:id", this.OnPlayerRemoved);
        chatRoom.Listen (this.OnChangeFallback);

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
                chatRoom.Send("some_command");
            }

            yield return 0;
        }

        OnApplicationQuit();
    }

    void OnOpenHandler (object sender, EventArgs e)
    {
        Debug.Log("Connected to server. Client id: " + colyseus.id);
    }

    void OnRoomJoined (object sender, EventArgs e)
    {
        Debug.Log("Joined room successfully.");
    }

	void OnAddPlayer (DataChange change)
    {
        Debug.Log ("OnAddPlayer");
		Debug.Log (change.path.ToString());
        Debug.Log (change.value);
    }

	void OnPlayerMove (DataChange change)
    {
        Debug.Log ("OnPlayerMove");
		Debug.Log ("playerId: " + change.path["id"] + ", Axis: " + change.path["axis"]);
		Debug.Log (change.value);
    }

	void OnPlayerRemoved (DataChange change)
    {
        Debug.Log ("OnPlayerRemoved");
		Debug.Log (change.path);
		Debug.Log (change.value);
    }

	void OnChangeFallback (PatchObject change)
    {
        Debug.Log ("OnChangeFallback");
		Debug.Log (change.operation);
		Debug.Log (change.path);
		Debug.Log (change.value);
    }

    void OnUpdateHandler (object sender, RoomUpdateEventArgs e)
    {
		Debug.Log("Update!");
        Debug.Log(e.state);
    }

    void OnApplicationQuit()
    {
        // Ensure the connection with server is closed immediatelly
        colyseus.Close();
    }
}

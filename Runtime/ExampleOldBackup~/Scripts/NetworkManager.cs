using System.Threading.Tasks;
using Colyseus;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static Client _client = null;
    private static MenuManager _menuManager = null;
    private static Room<MyRoomState> _room = null;

    public void Initialize()
    {
        if (_menuManager == null)
        {
            _menuManager = gameObject.AddComponent<MenuManager>();
        }

        _client = new Client(_menuManager.HostAddress);
    }

    public async Task JoinOrCreateGame()
    {
        // Will create a new game room if there is no available game rooms in the server.
        _room = await Client.JoinOrCreate<MyRoomState>(_menuManager.GameName);
    }

    public Client Client
    {
        get
        {
            // Initialize Colyseus client, if the client has not been initiated yet or input values from the Menu have been changed.
            if (_client == null || !_client.Settings.WebRequestEndpoint.Contains(_menuManager.HostAddress))
            {
                Initialize();
            }
            return _client;
        }
    }

    public Room<MyRoomState> GameRoom
    {
        get
        {
            if (_room == null)
            {
                Debug.LogError("Room hasn't been initialized yet!");
            }
            return _room;
        }
    }

    public void PlayerPosition(Vector2 position)
    {
        _ = GameRoom.Send("position", new { x = position.x, y = position.y });
    }
}

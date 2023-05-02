using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10f;
    private bool _moving;
    private NetworkManager _networkManager;
    private Vector2 _targetPosition;

    public async void Start()
    {
        // Initialize game room.
        _networkManager = gameObject.AddComponent<NetworkManager>();
        await _networkManager.JoinOrCreateGame();
        
        // Assigning listener for incoming messages
        _networkManager.GameRoom.OnMessage<string>("welcomeMessage", message =>
        {
            Debug.Log(message);
        });
        
        // Set player's new position after synchronized the mouse click's position with the Colyseus server. 
        _networkManager.GameRoom.State.OnChange += (changes) =>
        {
            var player = _networkManager.GameRoom.State.players[_networkManager.GameRoom.SessionId];
            _targetPosition = new Vector2(player.x, player.y);
            _moving = true;
        };

        _networkManager.GameRoom.State.players.OnAdd += (key, player) =>
        {
            Debug.Log($"Player {key} has joined the Game!");
        };
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Synchronize mouse click position with the Colyseus server.
            _networkManager.PlayerPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }

        if (_moving && (Vector2)transform.position != _targetPosition)
        {
            var step = speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, _targetPosition, step);
        }
        else
        {
            _moving = false;
        }
    }
}

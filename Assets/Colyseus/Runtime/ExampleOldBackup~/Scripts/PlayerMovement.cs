using System;
using System.Threading.Tasks;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float speed = 10f;
	private bool _moving;
	private NetworkManager _networkManager;
	private Vector2 _targetPosition;
	private StateCallbackStrategy<MyRoomState> _callbacks;

	private void Awake()
	{
		_networkManager = gameObject.AddComponent<NetworkManager>();
	}

	private async void Start()
	{
		await _networkManager.JoinOrCreateGame();
		registerListeners();
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

	private void registerListeners()
	{
		// Retrieve the StateCallbackStrategy object
		_callbacks = Callbacks.Get(_networkManager.GameRoom);

		// Assign listener for incoming messages
		_networkManager.GameRoom.OnMessage<string>("welcomeMessage", message =>
		{
			Debug.Log(message);
		});

		// Assign listeners for state change/add/remove
		_callbacks.OnAdd(state => state.players, (sessionId, addedPlayer) =>
		{
			Debug.Log($"player {sessionId} joined)");

			_callbacks.OnChange(addedPlayer, () =>
			{
				_targetPosition = new Vector2(addedPlayer.x, addedPlayer.y);
				_moving = true;
			});
		});

		_callbacks.OnRemove(state => state.players, (sessionId, removedPlayer) =>
		{
			Debug.Log($"player {sessionId} left");
		});
	}
}
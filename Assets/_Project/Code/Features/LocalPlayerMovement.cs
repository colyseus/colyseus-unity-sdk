using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class LocalPlayerMovement : MonoBehaviour, LocalPlayerInputActions.IPlayerActions
{
    [Inject] private readonly INetworkService _networkService;

    [SerializeField] private float _speed = 10f;
    private LocalPlayerInputActions _input;
    private Vector2 _move;
    private Vector2 _targetPosition = Vector2.zero;
    private Vector2 _currentPosition = Vector2.zero;

    private void Awake()
    {
        _input = new LocalPlayerInputActions();
        _input.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        _input.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _move = context.ReadValue<Vector2>();
    }

    private async void Update()
    {
        if (_move != Vector2.zero)
        {
            _targetPosition = new Vector2(transform.position.x, transform.position.y) + _move.normalized;
            var step = _speed * Time.deltaTime;
            _currentPosition = Vector2.MoveTowards(transform.position, _targetPosition, step);

            await _networkService.SendPosition(_currentPosition);
        }
    }

    private void LateUpdate()
    {
        transform.position = _currentPosition;
    }

    private void OnDisable()
    {
        _input.Disable();
    }

    private void OnDestroy()
    {
        _input.Player.RemoveCallbacks(this);
        _input = null;
    }
}
using UnityEngine;

public class NetworkedPlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    private Vector2 _targetPosition = Vector2.zero;

    public void SetTargetPosition(Vector2 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    private void Update()
    {
        if (_targetPosition != Vector2.zero)
        {
            var step = _speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, _targetPosition, step);
        }
    }
}
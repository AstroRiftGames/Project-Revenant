using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitAnimationController : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    [SerializeField] private Animator _animator;
    [SerializeField] private UnitMovement _movement;
    [SerializeField] private Vector2 _defaultFacingDirection = Vector2.down;
    [SerializeField] private float _attackStateDuration = 0.15f;

    private Vector2 _lastFacingDirection;
    private Vector2 _currentMovement;
    private float _attackStateUntilTime;

    public Vector2 LastFacingDirection => _lastFacingDirection;

    private void Awake()
    {
        _movement ??= GetComponent<UnitMovement>();
        _animator ??= GetComponentInChildren<Animator>();
        _lastFacingDirection = ResolveInitialFacingDirection();
        ApplyAnimatorState(isMoving: false);
    }

    private void Update()
    {
        Vector2 movement = _movement != null ? _movement.CurrentMovementDirection : Vector2.zero;
        SetMovement(movement);
    }

    public void SetMovement(Vector2 movement)
    {
        _currentMovement = SnapToEightDirections(movement);
        bool isMoving = _currentMovement.sqrMagnitude > 0f;

        if (isMoving)
            _lastFacingDirection = _currentMovement;

        ApplyAnimatorState(isMoving);
    }

    public void SetAttackTarget(Vector3 targetPosition)
    {
        Vector3 delta = targetPosition - transform.position;
        Vector2 attackDirection = SnapToEightDirections(new Vector2(delta.x, delta.y));
        if (attackDirection.sqrMagnitude > 0f)
            _lastFacingDirection = attackDirection;

        _attackStateUntilTime = Time.time + Mathf.Max(0f, _attackStateDuration);
        ApplyAnimatorState(isMoving: _currentMovement.sqrMagnitude > 0f);
    }

    private void ApplyAnimatorState(bool isMoving)
    {
        if (_animator == null)
            return;

        Vector2 facing = isMoving ? _currentMovement : _lastFacingDirection;
        bool isAttacking = Time.time < _attackStateUntilTime;

        _animator.SetFloat(MoveXHash, facing.x);
        _animator.SetFloat(MoveYHash, facing.y);
        _animator.SetBool(IsMovingHash, isMoving);
        _animator.SetBool(IsAttackingHash, isAttacking);
    }

    private Vector2 ResolveInitialFacingDirection()
    {
        Vector2 snappedDefault = SnapToEightDirections(_defaultFacingDirection);
        if (snappedDefault.sqrMagnitude > 0f)
            return snappedDefault;

        return Vector2.down;
    }

    private static Vector2 SnapToEightDirections(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        float radians = snappedAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
    }
}

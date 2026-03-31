using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitCombat))]
public class UnitBrain : MonoBehaviour
{
    [SerializeField] private float _retargetInterval = 0.25f;
    [SerializeField] private bool _runOnStart = true;
    [SerializeField] private bool _debugBrain;

    private Unit _unit;
    private UnitMovement _unitMovement;
    private UnitCombat _unitCombat;
    private Unit _currentTarget;
    private float _retargetTimer;
    private BrainState _brainState;

    private enum BrainState
    {
        Idle,
        Chase,
        Attack
    }

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _unitMovement = GetComponent<UnitMovement>();
        _unitCombat = GetComponent<UnitCombat>();
    }

    private void Update()
    {
        if (!_runOnStart || _unit == null || _unitMovement == null || _unitCombat == null)
            return;

        _retargetTimer -= Time.deltaTime;
        if (_retargetTimer > 0f)
            return;

        _retargetTimer = _retargetInterval;
        UpdateTarget();
        UpdateMovement();
    }

    private void UpdateTarget()
    {
        if (_currentTarget != null && _currentTarget.IsAlive && _unit.IsHostileTo(_currentTarget))
            return;

        Unit previousTarget = _currentTarget;
        _currentTarget = _unit.GetNearestVisibleHostileUnitInScene();

        if (!_debugBrain || previousTarget == _currentTarget)
            return;

        if (_currentTarget == null)
        {
            Debug.Log($"[Brain] {_unit.Id} lost target.", this);
            return;
        }

        Debug.Log($"[Brain] {_unit.Id} acquired target {_currentTarget.Id}.", this);
    }

    private void UpdateMovement()
    {
        if (_currentTarget == null)
        {
            _unitMovement.ClearDestination();
            SetBrainState(BrainState.Idle);
            return;
        }

        if (_unitCombat.IsTargetInRange(_currentTarget))
        {
            _unitMovement.ClearPath();
            SetBrainState(BrainState.Attack);
            _unitCombat.TryAttack(_currentTarget);
            return;
        }

        SetBrainState(BrainState.Chase);
        _unitMovement.SetTarget(_currentTarget, _unitCombat.AttackRangeInCells);
    }

    private void SetBrainState(BrainState nextState)
    {
        if (_brainState == nextState)
            return;

        _brainState = nextState;

        if (!_debugBrain)
            return;

        string targetLabel = _currentTarget != null ? _currentTarget.Id : "none";
        Debug.Log($"[Brain] {_unit.Id} -> {_brainState} (target: {targetLabel})", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (_currentTarget == null)
            return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, _currentTarget.Position);
    }
}

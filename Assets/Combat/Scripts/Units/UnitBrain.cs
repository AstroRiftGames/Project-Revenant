using Core.Audio;
using Core.Audio.Data;
using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TargetingStrategy))]
public class UnitBrain : MonoBehaviour
{
    private const int FearDesiredDistanceInCells = 999;

    [SerializeField] private bool _debugEncounter = true;
    [SerializeField] private bool _debugSkillFlow;
    [SerializeField] private bool _debugOperationalState;

    private Unit _unit;
    private UnitMovement _movement;
    private TargetingStrategy _targeting;
    private SkillCaster _skillCaster;
    private UnitAnimationController _animationController;
    private Unit _basicActionTargetUnit;
    private UnitOperationalState _lastOperationalState = UnitOperationalState.Idle;
    private bool _hasLoggedMissingControllerBlock;
    private bool _hasLoggedDeploymentBlock;
    private bool _hasLoggedResolvedBlock;
    private bool _hasLoggedStatusBlock;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _movement = GetComponent<UnitMovement>();
        _targeting = GetComponent<TargetingStrategy>();
        _skillCaster = GetComponent<SkillCaster>();
        _animationController = GetComponent<UnitAnimationController>();
    }

    private void Update()
    {
        if (!CanUpdateBrain())
            return;

        if (!CanActInCurrentEncounter())
            return;

        UnitOperationalState operationalState = _unit.OperationalState;
        LogOperationalStateChange(operationalState);
        if (!CanActFromOperationalState(operationalState))
            return;

        UpdateDecisionState();
        ExecuteDecision();
    }

    private bool CanUpdateBrain()
    {
        return _unit != null &&
               _movement != null &&
               _targeting != null &&
               _unit.Action != null &&
               _unit.LifecycleState == UnitLifecycleState.Alive &&
               _unit.IsAlive;
    }

    private void UpdateDecisionState()
    {
        _basicActionTargetUnit = _targeting.SelectBasicActionTarget(_unit, _unit.Action, _basicActionTargetUnit);
    }

    private void ExecuteDecision()
    {
        if (TryResolveFearBehavior())
            return;

        if (TryUseSkillIntent())
            return;

        if (TryExecuteBasicActionIntent())
            return;

        ExecuteMovementIntent();
    }

    private bool TryUseSkillIntent()
    {
        if (_skillCaster == null || !_skillCaster.IsSkillReady || !_skillCaster.TryUse(_basicActionTargetUnit))
            return false;

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} consumed action with skill before base attack.");
        return true;
    }

    private bool TryExecuteBasicActionIntent()
    {
        if (_basicActionTargetUnit == null)
            return false;

        if (!_unit.Action.IsInRange(_unit, _basicActionTargetUnit))
            return false;

        if (!_unit.Action.CanExecute(_unit, _basicActionTargetUnit))
            return false;

        ExecuteBasicAction();
        return true;
    }

    private void ExecuteMovementIntent()
    {
        if (TryMoveToBasicActionRange())
            return;

        TryMaintainSpacing();
    }

    private bool TryMoveToBasicActionRange()
    {
        Unit moveTargetUnit = ResolveMoveTargetUnit();
        if (moveTargetUnit == null)
            return false;

        if (_unit.Action.IsInRange(_unit, moveTargetUnit))
            return false;

        int preferredDistance = _unit.GetPreferredDistance(_unit.Action);
        return _movement.MoveTowards(moveTargetUnit, preferredDistance);
    }

    private Unit ResolveMoveTargetUnit()
    {
        if (!CanResolveMoveTargetUnit())
            return null;

        return SpacingEvaluator.GetNearestVisibleHostile(_unit);
    }

    private bool CanResolveMoveTargetUnit()
    {
        return _unit != null &&
               _unit.IsAlive &&
               _unit.LifecycleState == UnitLifecycleState.Alive &&
               _unit.RoomContext != null &&
               _unit.RoomContext.RoomGrid != null &&
               (_unit.StatusEffects == null || _unit.StatusEffects.CanMoveTowardTarget);
    }

    private void ExecuteBasicAction()
    {
        _animationController?.SetAttackTarget(_basicActionTargetUnit.Position);
        _unit.GetUnitData().AudioSet.TryGetClip("Attack", out AudioClipConfig clip);
        AudioService.Instance.PlaySFX(clip, transform.position);

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} fell back to base action against {FormatUnitIdentity(_basicActionTargetUnit)}.");
        _unit.BeginBasicActionExecution();
        try
        {
            _unit.Action.Execute(_unit, _basicActionTargetUnit);
        }
        finally
        {
            _unit.EndBasicActionExecution();
        }
    }

    private bool TryResolveFearBehavior()
    {
        StatusEffectController statusEffects = _unit != null ? _unit.StatusEffects : null;
        if (statusEffects == null || !statusEffects.ShouldFlee)
            return false;

        if (_movement.IsMoving)
            return true;

        Unit nearestThreat = SpacingEvaluator.GetNearestVisibleHostile(_unit);
        if (nearestThreat == null)
            return true;

        _movement.MoveAway(nearestThreat, FearDesiredDistanceInCells);
        return true;
    }

    private bool TryMaintainSpacing()
    {
        int preferredDistance = _unit.GetPreferredDistance(_unit.Action);
        Unit spacingThreat = SpacingEvaluator.GetSpacingThreat(_unit, _basicActionTargetUnit);
        return TryMaintainSpacingFromThreat(spacingThreat, preferredDistance);
    }

    private bool TryMaintainSpacingFromThreat(Unit threat, int preferredDistance)
    {
        if (threat == null || preferredDistance <= 0)
            return false;

        if (!_movement.IsWithinRange(threat, preferredDistance - 1))
            return false;

        return _movement.MoveAway(threat, preferredDistance);
    }

    private bool CanActInCurrentEncounter()
    {
        RoomContext roomContext = _unit != null ? _unit.RoomContext : null;
        if (roomContext == null)
            return true;

        CombatRoomController combatRoomController = roomContext.CombatController;
        if (combatRoomController == null)
        {
            if (!roomContext.IsCombatRoom)
                return true;

            LogEncounterGate(
                ref _hasLoggedMissingControllerBlock,
                $"[UnitBrain] '{name}' blocked in combat room '{roomContext.name}' because no CombatRoomController was resolved.");
            return false;
        }

        if (combatRoomController.CanUnitsAct)
        {
            ResetEncounterGateLogs();
            return true;
        }

        if (combatRoomController.State == CombatRoomState.Deployment)
        {
            LogEncounterGate(
                ref _hasLoggedDeploymentBlock,
                $"[UnitBrain] '{name}' blocked in room '{roomContext.name}'. Combat state: {combatRoomController.State}.");
            return false;
        }

        if (combatRoomController.State == CombatRoomState.Resolved)
        {
            LogEncounterGate(
                ref _hasLoggedResolvedBlock,
                $"[UnitBrain] '{name}' blocked in room '{roomContext.name}'. Combat state: {combatRoomController.State}.");
            return false;
        }

        return false;
    }

    private bool CanActFromOperationalState(UnitOperationalState operationalState)
    {
        switch (operationalState)
        {
            case UnitOperationalState.Dead:
                return false;

            case UnitOperationalState.CrowdControl:
                if (_unit != null && _unit.StatusEffects != null && _unit.StatusEffects.RestrictsMovement)
                    _movement.InterruptMovement();

                LogEncounterGate(
                    ref _hasLoggedStatusBlock,
                    $"[UnitBrain] '{name}' blocked by active status effect.");
                return false;

            case UnitOperationalState.Casting:
            case UnitOperationalState.Attacking:
            case UnitOperationalState.Moving:
                return false;

            default:
                return true;
        }
    }

    private void LogEncounterGate(ref bool guard, string message)
    {
        if (!_debugEncounter || guard)
            return;

        guard = true;
        Debug.Log(message, this);
    }

    private void ResetEncounterGateLogs()
    {
        _hasLoggedMissingControllerBlock = false;
        _hasLoggedDeploymentBlock = false;
        _hasLoggedResolvedBlock = false;
        _hasLoggedStatusBlock = false;
    }

    private void LogSkillFlow(string message)
    {
        if (_debugSkillFlow)
            Debug.Log(message, this);
    }

    private void LogOperationalStateChange(UnitOperationalState operationalState)
    {
        if (!_debugOperationalState || _lastOperationalState == operationalState)
            return;

        _lastOperationalState = operationalState;
        Debug.Log($"[UnitBrain] '{name}' operational state -> {operationalState}.", this);
    }

    private string FormatDebugIdentity()
    {
        if (_unit == null)
            return $"[{name}#{GetInstanceID()}|NoUnit]";

        string unitId = !string.IsNullOrWhiteSpace(_unit.Id) ? _unit.Id : "NoUnitId";
        return $"[{_unit.name}#{_unit.GetInstanceID()}|{unitId}]";
    }

    private static string FormatUnitIdentity(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }
}

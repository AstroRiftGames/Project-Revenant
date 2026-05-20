using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TargetingStrategy))]
public class UnitBrain : MonoBehaviour
{
    private const int FearDesiredDistanceInCells = 999;

    [SerializeField] private bool _debugEncounter = true;
    [SerializeField] private bool _debugSkillFlow;

    private Unit _unit;
    private UnitMovement _movement;
    private TargetingStrategy _targeting;
    private SkillCaster _skillCaster;
    private UnitAnimationController _animationController;
    private IBasicAction _action;
    private Unit _currentTarget;
    private bool _hasLoggedMissingControllerBlock;
    private bool _hasLoggedDeploymentBlock;
    private bool _hasLoggedResolvedBlock;
    private bool _hasLoggedStatusBlock;

    private enum BrainExecutionResult
    {
        NoOp,
        Consumed
    }

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _movement = GetComponent<UnitMovement>();
        _targeting = GetComponent<TargetingStrategy>();
        _skillCaster = GetComponent<SkillCaster>();
        _animationController = GetComponent<UnitAnimationController>();
        _action = _unit != null ? _unit.Action : null;
    }

    private void Update()
    {
        if (!CanUpdateBrain())
            return;

        if (!CanActFromStatusEffects())
            return;

        if (!CanActInCurrentEncounter())
            return;

        if (_skillCaster != null && _skillCaster.IsCasting)
            return;

        if (_movement.IsMoving)
            return;

        UpdateDecisionState();
        ExecuteDecision();
    }

    private bool CanUpdateBrain()
    {
        return _unit != null &&
               _movement != null &&
               _targeting != null &&
               _action != null &&
               _unit.IsAlive;
    }

    private void UpdateDecisionState()
    {
        _currentTarget = _targeting.SelectBasicActionTarget(_unit, _action, _currentTarget);
    }

    private void ExecuteDecision()
    {
        if (ExecuteImmediateIntent() == BrainExecutionResult.Consumed)
            return;

        ExecuteCombatIntent();
    }

    private BrainExecutionResult ExecuteImmediateIntent()
    {
        if (TryResolveFearBehavior())
            return BrainExecutionResult.Consumed;

        if (TryMaintainSpacing())
            return BrainExecutionResult.Consumed;

        return BrainExecutionResult.NoOp;
    }

    private void ExecuteCombatIntent()
    {
        if (TryUseSkillIntent())
            return;

        ExecuteBasicActionIntent();
    }

    private bool TryUseSkillIntent()
    {
        if (_skillCaster == null || !_skillCaster.IsSkillReady || !_skillCaster.TryUse(_currentTarget))
            return false;

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} consumed action with skill before base attack.");
        return true;
    }

    private void ExecuteBasicActionIntent()
    {
        if (_currentTarget == null)
            return;

        if (TryMoveToBasicActionRange())
            return;

        if (!_action.IsInRange(_unit, _currentTarget))
        {
            TryRetargetAfterFailedMovement();
            return;
        }

        if (!_action.CanExecute(_unit, _currentTarget))
            return;

        ExecuteBasicAction();
    }

    private bool TryMoveToBasicActionRange()
    {
        if (_currentTarget == null)
            return false;

        if (_action.IsInRange(_unit, _currentTarget))
            return false;

        int preferredDistance = _unit.GetPreferredDistance(_action);
        return _movement.MoveTowards(_currentTarget, preferredDistance);
    }

    private bool TryRetargetAfterFailedMovement()
    {
        if (_targeting == null || _currentTarget == null)
            return false;

        Unit failedTarget = _currentTarget;
        Unit alternateTarget = _targeting.SelectAlternativeBasicActionTarget(_unit, _action, failedTarget);
        if (alternateTarget == null || ReferenceEquals(alternateTarget, failedTarget))
            return false;

        _currentTarget = alternateTarget;
        return true;
    }

    private void ExecuteBasicAction()
    {
        _animationController?.SetAttackTarget(_currentTarget.Position);

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} fell back to base action against {FormatUnitIdentity(_currentTarget)}.");
        _action.Execute(_unit, _currentTarget);
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

        _currentTarget = nearestThreat;
        _movement.MoveAway(nearestThreat, FearDesiredDistanceInCells);
        return true;
    }

    private bool TryMaintainSpacing()
    {
        int preferredDistance = _unit.GetPreferredDistance(_action);
        Unit spacingThreat = SpacingEvaluator.GetSpacingThreat(_unit, _currentTarget);
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

    private bool CanActFromStatusEffects()
    {
        if (_unit == null || _unit.StatusEffects == null || _unit.StatusEffects.CanAct)
            return true;

        if (_unit.StatusEffects.RestrictsMovement)
            _movement.InterruptMovement();

        LogEncounterGate(
            ref _hasLoggedStatusBlock,
            $"[UnitBrain] '{name}' blocked by active status effect.");
        return false;
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

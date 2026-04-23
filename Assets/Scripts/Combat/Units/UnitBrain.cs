using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TargetingStrategy))]
public class UnitBrain : MonoBehaviour
{
    [SerializeField] private bool _debugEncounter = true;
    [SerializeField] private bool _debugSkillFlow;

    private Unit _unit;
    private UnitMovement _movement;
    private TargetingStrategy _targeting;
    private SkillCaster _skillCaster;
    private IAction _action;
    private Unit _currentTarget;
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
        _action = _unit != null ? _unit.Action : null;
    }

    private void Update()
    {
        if (_unit == null || _movement == null || _targeting == null || _action == null || !_unit.IsAlive)
            return;

        if (!CanActInCurrentEncounter())
            return;

        if (_movement.IsMoving)
            return;

        _currentTarget = _targeting.SelectTarget(_unit, _currentTarget);
        int preferredDistance = _unit.GetPreferredDistance(_action);
        Unit spacingThreat = _targeting.GetSpacingThreat(_unit, _currentTarget);

        if (TryMaintainSpacing(spacingThreat, preferredDistance))
            return;

        if (_currentTarget == null)
            return;

        if (!_action.IsInRange(_unit, _currentTarget))
        {
            _movement.MoveTowards(_currentTarget, preferredDistance);
            return;
        }

        if (!_action.CanExecute(_unit, _currentTarget))
            return;

        if (_skillCaster != null && _skillCaster.TryUse(_currentTarget))
        {
            LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} consumed action with skill before base attack.");
            return;
        }

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} fell back to base action against {FormatUnitIdentity(_currentTarget)}.");
        _action.Execute(_unit, _currentTarget);
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
            if (_unit.StatusEffects != null && !_unit.StatusEffects.CanAct)
            {
                if (_unit.StatusEffects.RestrictsMovement)
                    _movement.InterruptMovement();

                LogEncounterGate(
                    ref _hasLoggedStatusBlock,
                    $"[UnitBrain] '{name}' blocked by active status effect.");
                return false;
            }

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

    private bool TryMaintainSpacing(Unit threat, int preferredDistance)
    {
        if (threat == null || preferredDistance <= 0)
            return false;

        if (!_movement.IsWithinRange(threat, preferredDistance - 1))
            return false;

        return _movement.MoveAway(threat, preferredDistance);
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

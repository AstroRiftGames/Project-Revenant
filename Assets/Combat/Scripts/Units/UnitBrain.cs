using System;
using System.Collections.Generic;
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

    private bool _hasSkillIntent;
    private Unit _skillIntentTarget;
    private int _skillIntentMoveFailures;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _movement = GetComponent<UnitMovement>();
        _targeting = GetComponent<TargetingStrategy>();
        _skillCaster = GetComponent<SkillCaster>();
        _animationController = GetComponent<UnitAnimationController>();
    }

    private void OnDisable()
    {
        ClearSkillIntent();
    }

    private void Update()
    {
        if (!CanUpdateBrain())
        {
            ClearSkillIntent();
            return;
        }

        if (!CanActInCurrentEncounter())
        {
            ClearSkillIntent();
            return;
        }

        if (_unit.StatusEffects != null && (_unit.StatusEffects.HasStun || _unit.StatusEffects.HasKnockback))
        {
            ClearSkillIntent();
        }

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

        // 1. Evaluar Skill Disponible
        if (TryEvaluateAndUseSkill())
            return;

        // 2. Si no hay skill (o no es viable), evaluar acción básica
        if (TryEvaluateAndUseBasicAction())
            return;

        // 3. Spacing si corresponde
        if (TryMaintainSpacing())
            return;

        // 4. Idle con razón clara
        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} remains Idle: no valid or reachable targets for active skills or basic actions.");
    }

    private bool TryEvaluateAndUseSkill()
    {
        if (_skillCaster != null && _skillCaster.Skill != null && _debugSkillFlow)
        {
            Debug.Log($"[UnitBrain Debug Flow] Evaluating skill '{_skillCaster.Skill.DisplayName}' for caster '{FormatDebugIdentity()}'. IsSkillReady: {_skillCaster.IsSkillReady}, Charge: {_skillCaster.CurrentCharge}/{_skillCaster.MaxCharge}", this);
        }

        if (_skillCaster == null || !_skillCaster.IsSkillReady)
        {
            ClearSkillIntent();
            return false;
        }

        SkillData skill = _skillCaster.Skill;
        if (skill != null && skill.PrimaryTargetRequirement == PrimaryTargetRequirement.GroundCell)
        {
            RoomGrid roomGrid = _unit.RoomContext != null ? _unit.RoomContext.RoomGrid : null;
            if (roomGrid != null)
            {
                Vector3Int casterCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, _unit);
                Unit referenceTarget = _basicActionTargetUnit != null ? _basicActionTargetUnit : _unit;
                Vector3Int referenceCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, referenceTarget);
                int rangeVal = _skillCaster.GetSkillRange();

                Vector3Int bestCell = Vector3Int.zero;
                float minDistance = float.MaxValue;
                bool cellFound = false;

                if (_debugSkillFlow)
                {
                    Debug.Log($"[UnitBrain Debug Flow] GroundCell skill '{skill.DisplayName}' evaluated for {FormatDebugIdentity()}. " +
                              $"CasterCell: {casterCell}, RefTarget: {FormatUnitIdentity(referenceTarget)}, RefCell: {referenceCell}", this);
                }

                for (int x = -rangeVal; x <= rangeVal; x++)
                {
                    for (int y = -rangeVal; y <= rangeVal; y++)
                    {
                        Vector3Int candidateCell = casterCell + new Vector3Int(x, y, 0);

                        if (!GridNavigationUtility.IsWithinCellRange(casterCell, candidateCell, rangeVal))
                            continue;

                        if (!roomGrid.HasCell(candidateCell) || 
                            !roomGrid.IsCellInsideWalkableBounds(candidateCell) || 
                            roomGrid.IsCellHardBlocked(candidateCell))
                            continue;

                        if (roomGrid.OccupancyService != null && 
                            !roomGrid.OccupancyService.IsCellFreeForPlacement(candidateCell))
                            continue;

                        float dist = GridNavigationUtility.GetCellDistance(referenceCell, candidateCell);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestCell = candidateCell;
                            cellFound = true;
                        }
                    }
                }

                if (!cellFound)
                {
                    if (_debugSkillFlow)
                    {
                        Debug.Log($"[UnitBrain Debug Flow] GroundCell skill '{skill.DisplayName}' aborted for {FormatDebugIdentity()}: no valid/walkable/unoccupied cells found in range ({rangeVal}).", this);
                    }
                    return false;
                }

                Vector2Int targetCell2D = new Vector2Int(bestCell.x, bestCell.y);
                bool castResult = _skillCaster.TryUseGroundCell(targetCell2D);
                if (_debugSkillFlow)
                {
                    Debug.Log($"[UnitBrain Debug Flow] TryUseGroundCell for '{skill.DisplayName}' at {targetCell2D} result: {castResult}", this);
                }

                if (castResult)
                {
                    ClearSkillIntent();
                    return true;
                }
            }
            return false;
        }

        Unit target = _skillCaster.GetPreferredSkillTarget(_basicActionTargetUnit);
        int skillRange = _skillCaster.GetSkillRange();

        if (_skillCaster.SkillRequiresTarget())
        {
            bool targetIsValid = target != null && _skillCaster.IsTargetSelectableForSkill(target);
            bool targetIsReachable = targetIsValid && _movement.CanReachTarget(target, skillRange);

            if (!targetIsReachable)
            {
                // Buscar otro objetivo válido y alcanzable en la sala
                Unit alternativeTarget = null;
                IReadOnlyList<Unit> candidates = _unit.GetRoomUnits();
                for (int i = 0; i < candidates.Count; i++)
                {
                    Unit candidate = candidates[i];
                    if (candidate == null || ReferenceEquals(candidate, target))
                        continue;

                    if (_skillCaster.IsTargetSelectableForSkill(candidate) && _movement.CanReachTarget(candidate, skillRange))
                    {
                        alternativeTarget = candidate;
                        break;
                    }
                }

                if (alternativeTarget != null)
                {
                    LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} target {FormatUnitIdentity(target)} unreachable. Switching skill target to {FormatUnitIdentity(alternativeTarget)}.");
                    target = alternativeTarget;
                }
                else
                {
                    LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} no reachable targets for skill. Degrading to basic action.");
                    ClearSkillIntent();
                    return false;
                }
            }

            if (_hasSkillIntent && _skillIntentTarget != target)
            {
                LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} changed skill intent target from {FormatUnitIdentity(_skillIntentTarget)} to {FormatUnitIdentity(target)}.");
                _skillIntentTarget = target;
                _skillIntentMoveFailures = 0;
            }

            if (_skillCaster.CanCastNow(target))
            {
                if (_skillCaster.TryUse(target))
                {
                    LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} casted skill against {FormatUnitIdentity(target)}.");
                    ClearSkillIntent();
                    return true;
                }
            }
            else
            {
                _hasSkillIntent = true;
                _skillIntentTarget = target;

                if (TryMoveToSkillRange())
                {
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }
        else
        {
            if (_skillCaster.CanCastNow(null))
            {
                if (_skillCaster.TryUse(null))
                {
                    LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} casted self/caster-centered skill.");
                    ClearSkillIntent();
                    return true;
                }
            }

            if (_hasSkillIntent)
            {
                LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} cleared skill intent: skill does not require target and cannot be cast now.");
                ClearSkillIntent();
            }
        }

        return false;
    }

    private bool TryEvaluateAndUseBasicAction()
    {
        ClearSkillIntent(); // Asegurar que no hay intención de skill activa

        Unit target = _basicActionTargetUnit;
        int actionRange = _unit.Action.RangeInCells;

        bool targetIsValid = target != null && _unit.Action.IsValidTarget(_unit, target);
        bool targetIsReachable = targetIsValid && _movement.CanReachTarget(target, actionRange);

        if (!targetIsReachable)
        {
            // Buscar otro objetivo básico alternativo en la sala
            Unit alternativeTarget = null;
            IReadOnlyList<Unit> candidates = _unit.GetRoomUnits();
            for (int i = 0; i < candidates.Count; i++)
            {
                Unit candidate = candidates[i];
                if (candidate == null || ReferenceEquals(candidate, target))
                    continue;

                if (_unit.Action.IsValidTarget(_unit, candidate) && _movement.CanReachTarget(candidate, actionRange))
                {
                    alternativeTarget = candidate;
                    break;
                }
            }

            if (alternativeTarget != null)
            {
                LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} target {FormatUnitIdentity(target)} unreachable. Switching basic target to {FormatUnitIdentity(alternativeTarget)}.");
                target = alternativeTarget;
                _basicActionTargetUnit = alternativeTarget;
            }
            else
            {
                return false;
            }
        }

        if (_unit.Action.IsInRange(_unit, target) && _unit.Action.CanExecute(_unit, target))
        {
            ExecuteBasicAction(target);
            return true;
        }
        else
        {
            int preferredDistance = _unit.GetPreferredDistance(_unit.Action);
            LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} moving to basic range of {FormatUnitIdentity(target)} (range={actionRange}, preferred={preferredDistance}).");
            return _movement.MoveTowards(target, preferredDistance);
        }
    }

    private bool TryMoveToSkillRange()
    {
        if (!_hasSkillIntent || _skillIntentTarget == null)
            return false;

        int range = _skillCaster.GetSkillRange();
        if (_movement.IsWithinRange(_skillIntentTarget, range))
            return false;

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} moving towards skill target {FormatUnitIdentity(_skillIntentTarget)} at range {range}.");
        MovementRequestResult result = _movement.RequestMoveTowards(_skillIntentTarget, range);

        if (result == MovementRequestResult.Failed)
        {
            _skillIntentMoveFailures++;
            if (_skillIntentMoveFailures >= 3)
            {
                LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} cleared skill intent: movement failed {_skillIntentMoveFailures} times.");
                ClearSkillIntent();
            }
            return false;
        }
        else if (result == MovementRequestResult.TemporarilyDelayed)
        {
            return true;
        }
        else // Accepted
        {
            _skillIntentMoveFailures = 0;
            return true;
        }
    }

    private void ClearSkillIntent()
    {
        _hasSkillIntent = false;
        _skillIntentTarget = null;
        _skillIntentMoveFailures = 0;
    }

    private void ExecuteBasicAction(Unit target)
    {
        _animationController?.SetAttackTarget(target.Position);
        AudioService.TryPlayClipFromSet(_unit.GetUnitData()?.AudioSet, "Attack", transform.position);

        LogSkillFlow($"[UnitBrain] {FormatDebugIdentity()} executed base action against {FormatUnitIdentity(target)}.");
        _unit.BeginBasicActionExecution();
        try
        {
            _unit.Action.Execute(_unit, target);
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

        Unit nearestThreat = TargetingStrategy.GetNearestVisibleHostile(_unit);
        if (nearestThreat == null)
            return true;

        _movement.MoveAway(nearestThreat, FearDesiredDistanceInCells);
        return true;
    }

    private bool TryMaintainSpacing()
    {
        int preferredDistance = _unit.GetPreferredDistance(_unit.Action);
        Unit spacingThreat = TargetingStrategy.GetSpacingThreat(_unit, _basicActionTargetUnit);
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
                ClearSkillIntent();
                return false;

            case UnitOperationalState.CrowdControl:
                if (_unit != null && _unit.StatusEffects != null && _unit.StatusEffects.RestrictsMovement)
                    _movement.InterruptMovement();

                ClearSkillIntent();
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
            return $"[{name}#{GetEntityId()}|NoUnit]";

        string unitId = !string.IsNullOrWhiteSpace(_unit.Id) ? _unit.Id : "NoUnitId";
        return $"[{_unit.name}#{_unit.GetEntityId()}|{unitId}]";
    }

    private static string FormatUnitIdentity(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetEntityId()}|{unitId}]";
    }
}

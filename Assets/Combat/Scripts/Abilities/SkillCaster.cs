using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
public class SkillCaster : MonoBehaviour
{
    [SerializeField] private SkillData _overrideSkill;
    [SerializeField] private bool _debugLogs;

    private Unit _unit;
    private SkillData _resolvedSkill;
    private readonly SkillState _state = new();
    private readonly List<Unit> _unitsHit = new();

    public event Action<Unit, SkillData, Unit> SkillUsed;

    public SkillData Skill => ResolveSkill();
    public bool HasSkill => Skill != null;
    public float CurrentCooldown => _state.RemainingCooldown;
    public float MaxCooldown => Skill != null ? Skill.Cooldown : 0f;
    public Sprite Icon => Skill != null ? Skill.Icon : null;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        ResolveSkill();
    }

    private void Update()
    {
        if (_state.IsReady || !CanChargeCooldown())
            return;

        _state.Tick(Time.deltaTime);
    }

    public bool TryUse(Unit combatTarget)
    {
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} attempting skill. Combat target: {FormatUnitName(combatTarget)}.");

        SkillData skill = ResolveSkill();
        if (!CanTryUseSkill(skill))
            return false;

        Unit selectedTarget = ChooseSkillTarget(skill, combatTarget);
        if (!TryValidateChosenTarget(skill, selectedTarget))
            return false;

        if (!IsTargetCloseEnough(skill, selectedTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(selectedTarget)} is out of range.");
            return false;
        }

        if (!TryCollectUnitsHit(skill, selectedTarget))
            return false;

        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} '{skill.DisplayName}' resolved target {FormatUnitName(selectedTarget)} " +
            $"and {_unitsHit.Count} target(s) hit: {FormatUnits(_unitsHit)}.");

        if (!ApplySkillToUnitsHit(skill, selectedTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' applied no effects to impacted targets.");
            return false;
        }

        OnSkillCastSucceeded(skill, selectedTarget);
        return true;
    }

    public void ResetState()
    {
        _state.Reset();
    }

    private bool CanChargeCooldown()
    {
        return _unit == null || _unit.StatusEffects == null || !_unit.StatusEffects.PreventsSkillCooldownCharge;
    }

    private bool CanTryUseSkill(SkillData skill)
    {
        if (_unit == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: owner unit was not resolved.");
            return false;
        }

        if (skill == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: no skill assigned.");
            return false;
        }

        if (_unit.StatusEffects != null && !_unit.StatusEffects.CanUseSkills)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: active status effect blocks skill usage.");
            return false;
        }

        if (_state.IsReady)
            return true;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' is on cooldown for {_state.RemainingCooldown:F2}s.");
        return false;
    }

    private SkillData ResolveSkill()
    {
        if (_overrideSkill != null)
            return _resolvedSkill = _overrideSkill;

        if (_resolvedSkill != null)
            return _resolvedSkill;

        UnitData unitData = _unit != null ? _unit.GetUnitData() : null;
        _resolvedSkill = unitData != null ? unitData.skill : null;
        return _resolvedSkill;
    }

    private Unit ChooseSkillTarget(SkillData skill, Unit combatTarget)
    {
        if (skill == null || _unit == null)
            return null;

        if (skill.ResolvesPrimaryTargetToCaster)
            return _unit.IsAlive ? _unit : null;

        if (CanChooseTarget(skill, combatTarget))
            return combatTarget;

        return FindFallbackTarget(skill);
    }

    private bool TryValidateChosenTarget(SkillData skill, Unit selectedTarget)
    {
        if (selectedTarget == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' resolved no valid target.");
            return false;
        }

        if (CanChooseTarget(skill, selectedTarget))
            return true;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(selectedTarget)} failed skill rules.");
        return false;
    }

    private bool CanChooseTarget(SkillData skill, Unit target)
    {
        if (skill == null || _unit == null)
            return false;

        if (skill.UsesCasterAsImpactCenter)
            return _unit.IsAlive;

        return SkillHitCollector.CanSkillHitUnit(_unit, skill, target, allowCasterForSelfCenteredSkill: true);
    }

    private bool IsTargetCloseEnough(SkillData skill, Unit selectedTarget)
    {
        if (_unit == null || skill == null)
            return false;

        if (selectedTarget == null)
            return !skill.RequiresTarget;

        if (skill.ResolvesPrimaryTargetToCaster)
            return true;

        RoomGrid grid = _unit.RoomContext != null ? _unit.RoomContext.RoomGrid : null;
        if (grid == null)
            return Vector3.Distance(_unit.Position, selectedTarget.Position) <= skill.RangeInCells;

        Vector3Int casterCell = GridUnitCellUtility.ResolveUnitCell(grid, _unit);
        Vector3Int targetCell = GridUnitCellUtility.ResolveUnitCell(grid, selectedTarget);
        return GridNavigationUtility.IsWithinCellRange(casterCell, targetCell, skill.RangeInCells);
    }

    private bool TryCollectUnitsHit(SkillData skill, Unit selectedTarget)
    {
        _unitsHit.Clear();
        if (SkillHitCollector.TryCollectTargets(_unit, skill, selectedTarget, _unitsHit, LogDebug))
            return true;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' shape '{skill.Shape}' produced no targets.");
        return false;
    }

    private bool ApplySkillToUnitsHit(SkillData skill, Unit selectedTarget)
    {
        if (skill == null || _unit == null || _unitsHit.Count == 0)
            return false;

        bool anyApplied = false;
        for (int targetIndex = 0; targetIndex < _unitsHit.Count; targetIndex++)
        {
            Unit hitUnit = _unitsHit[targetIndex];
            if (hitUnit == null)
                continue;

            anyApplied |= ApplySkillToUnit(skill, selectedTarget, hitUnit);
        }

        return anyApplied;
    }

    private bool ApplySkillToUnit(SkillData skill, Unit selectedTarget, Unit hitUnit)
    {
        if (skill == null || hitUnit == null)
            return false;

        bool anyApplied = false;

        anyApplied |= ApplySkillEffectsToUnit(skill, selectedTarget, hitUnit);
        anyApplied |= ApplySkillStatusesToUnit(skill, hitUnit);
        return anyApplied;
    }

    private bool ApplySkillEffectsToUnit(SkillData skill, Unit selectedTarget, Unit hitUnit)
    {
        SkillEffect[] effects = skill.Effects;
        if (effects == null || effects.Length == 0)
            return false;

        bool anyApplied = false;
        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
        {
            SkillEffect effect = effects[effectIndex];
            if (effect == null)
                continue;

            anyApplied |= effect.Apply(_unit, skill, selectedTarget, hitUnit);
        }

        return anyApplied;
    }

    private bool ApplySkillStatusesToUnit(SkillData skill, Unit hitUnit)
    {
        SkillStatusEffect[] statusEffects = skill.StatusEffects;
        if (statusEffects == null || statusEffects.Length == 0 || hitUnit == null || hitUnit.StatusEffects == null)
            return false;

        bool anyApplied = false;
        for (int effectIndex = 0; effectIndex < statusEffects.Length; effectIndex++)
        {
            SkillStatusEffect statusEffect = statusEffects[effectIndex];
            StatusEffectDefinition definition = statusEffect.Definition;
            if (definition == null || !CanApplyStatusToUnit(statusEffect.TargetRelation, hitUnit))
                continue;

            StatusEffectApplication application = new(hitUnit, _unit, skill, definition);
            anyApplied |= hitUnit.StatusEffects.TryApply(application);
        }

        return anyApplied;
    }

    private bool CanApplyStatusToUnit(TargetRelation targetRelation, Unit hitUnit)
    {
        if (_unit == null || hitUnit == null)
            return false;

        return targetRelation switch
        {
            TargetRelation.Hostile => _unit.IsHostileTo(hitUnit),
            TargetRelation.Ally => !_unit.IsHostileTo(hitUnit),
            _ => true
        };
    }

    private Unit FindFallbackTarget(SkillData skill)
    {
        if (_unit == null || skill == null)
            return null;

        SkillTargetRequirement targetType = skill.Requirements != null
            ? skill.Requirements.TargetRequirement
            : SkillTargetRequirement.Any;

        return targetType switch
        {
            SkillTargetRequirement.Self => CanChooseTarget(skill, _unit) ? _unit : null,
            SkillTargetRequirement.Ally => FindMostInjuredTarget(skill),
            _ => FindClosestTarget(skill)
        };
    }

    private Unit FindClosestTarget(SkillData skill)
    {
        IReadOnlyList<Unit> roomUnits = _unit.GetRoomUnits();
        Unit bestTarget = null;
        float bestDistance = float.MaxValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!CanChooseTarget(skill, candidate))
                continue;

            float sqrDistance = (_unit.Position - candidate.Position).sqrMagnitude;
            if (sqrDistance > bestDistance)
                continue;

            if (Mathf.Approximately(sqrDistance, bestDistance) && candidate.CurrentHealth >= bestHealth)
                continue;

            bestTarget = candidate;
            bestDistance = sqrDistance;
            bestHealth = candidate.CurrentHealth;
        }

        return bestTarget;
    }

    private Unit FindMostInjuredTarget(SkillData skill)
    {
        IReadOnlyList<Unit> roomUnits = _unit.GetRoomUnits();
        Unit bestTarget = null;
        float bestHealthRatio = float.MaxValue;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!CanChooseTarget(skill, candidate))
                continue;

            float healthRatio = candidate.MaxHealth > 0
                ? (float)candidate.CurrentHealth / candidate.MaxHealth
                : 1f;
            float sqrDistance = (_unit.Position - candidate.Position).sqrMagnitude;

            if (healthRatio > bestHealthRatio)
                continue;

            if (Mathf.Approximately(healthRatio, bestHealthRatio) && sqrDistance >= bestDistance)
                continue;

            bestTarget = candidate;
            bestHealthRatio = healthRatio;
            bestDistance = sqrDistance;
        }

        return bestTarget;
    }

    private void OnSkillCastSucceeded(SkillData skill, Unit selectedTarget)
    {
        BreakInvisibilityAfterSkillUse();
        ConsumeSkillCooldown(skill);
        NotifySkillUsed(skill, ResolvePopupAnchor(skill, selectedTarget));
    }

    private void BreakInvisibilityAfterSkillUse()
    {
        if (_unit != null && _unit.StatusEffects != null && _unit.StatusEffects.HasInvisibility)
            _unit.StatusEffects.RemoveEffectOfType(StatusEffectType.Invisibility);
    }

    private void ConsumeSkillCooldown(SkillData skill)
    {
        if (skill == null)
            return;

        _state.StartCooldown(skill.Cooldown);
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} started cooldown for '{skill.DisplayName}': {skill.Cooldown:F2}s.");
    }

    private Unit ResolvePopupAnchor(SkillData skill, Unit selectedTarget)
    {
        if (skill != null && skill.UsesCasterAsPresentationAnchor)
            return _unit;

        if (selectedTarget != null)
            return selectedTarget;

        return _unitsHit.Count > 0 ? _unitsHit[0] : null;
    }

    private void NotifySkillUsed(SkillData skill, Unit popupAnchor)
    {
        int listenerCount = SkillUsed?.GetInvocationList().Length ?? 0;
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} emitting SkillUsed for '{skill.DisplayName}' with {listenerCount} listener(s).");
        SkillUsed?.Invoke(_unit, skill, popupAnchor);
        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} used '{skill.DisplayName}' on {_unitsHit.Count} target(s) hit.");
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }

    private string FormatOwnerIdentity()
    {
        Unit owner = _unit;
        string ownerName = owner != null ? owner.name : name;
        int ownerInstanceId = owner != null ? owner.GetInstanceID() : GetInstanceID();
        string unitId = owner != null && !string.IsNullOrWhiteSpace(owner.Id) ? owner.Id : "NoUnitId";
        return $"[{ownerName}#{ownerInstanceId}|{unitId}]";
    }

    private static string FormatUnitName(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }

    private static string FormatUnits(IReadOnlyList<Unit> units)
    {
        if (units == null || units.Count == 0)
            return "[None]";

        string[] labels = new string[units.Count];
        for (int i = 0; i < units.Count; i++)
            labels[i] = FormatUnitName(units[i]);

        return string.Join(", ", labels);
    }
}

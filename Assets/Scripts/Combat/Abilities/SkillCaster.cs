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
    private readonly List<Unit> _impactedUnitsBuffer = new();

    public event Action<SkillCastContext, Unit> SkillUsed;

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

        if (!_state.IsReady)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' is on cooldown for {_state.RemainingCooldown:F2}s.");
            return false;
        }

        Unit primaryTarget = ResolvePrimaryTarget(skill, combatTarget);
        if (primaryTarget == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' resolved no target for mode {skill.TargetMode}.");
            return false;
        }

        if (!IsTargetValid(skill, primaryTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(primaryTarget)} failed requirements.");
            return false;
        }

        if (!IsInRange(skill, primaryTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(primaryTarget)} is out of range.");
            return false;
        }

        SkillCastContext context = new(_unit, skill, primaryTarget);
        _impactedUnitsBuffer.Clear();
        if (!SkillTargetCollector.TryCollectTargets(context, _impactedUnitsBuffer, LogDebug))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' shape '{skill.Shape}' produced no impacted targets.");
            return false;
        }

        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} '{skill.DisplayName}' resolved primary target {FormatUnitName(primaryTarget)} " +
            $"and {_impactedUnitsBuffer.Count} impacted unit(s): {FormatUnits(_impactedUnitsBuffer)}.");

        if (!ApplyEffects(skill, context, _impactedUnitsBuffer))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' applied no effects to impacted targets.");
            return false;
        }

        if (_unit.StatusEffects != null && _unit.StatusEffects.HasInvisibility)
            _unit.StatusEffects.RemoveEffectOfType(StatusEffectType.Invisibility);

        _state.StartCooldown(skill.Cooldown);
        int listenerCount = SkillUsed?.GetInvocationList().Length ?? 0;
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} emitting SkillUsed for '{skill.DisplayName}' with {listenerCount} listener(s).");
        SkillUsed?.Invoke(context, primaryTarget);
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} used '{skill.DisplayName}' on {_impactedUnitsBuffer.Count} impacted unit(s). Primary target: {FormatUnitName(primaryTarget)}.");
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} started cooldown for '{skill.DisplayName}': {skill.Cooldown:F2}s.");
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

    private Unit ResolvePrimaryTarget(SkillData skill, Unit combatTarget)
    {
        if (skill == null || _unit == null)
            return null;

        return skill.TargetMode switch
        {
            SkillTargetMode.Self => _unit,
            _ => combatTarget
        };
    }

    private bool IsTargetValid(SkillData skill, Unit primaryTarget)
    {
        if (skill == null)
            return false;

        if (IsSelfCenteredAreaSkill(skill))
            return _unit != null && _unit.IsAlive;

        SkillRequirements requirements = skill.Requirements;
        return requirements == null || requirements.AreMet(_unit, primaryTarget);
    }

    private bool IsInRange(SkillData skill, Unit primaryTarget)
    {
        if (_unit == null || skill == null)
            return false;

        if (primaryTarget == null)
            return !ResolveRequiresRangeCheck(skill);

        int rangeInCells = skill.RangeInCells;
        if (skill.TargetMode == SkillTargetMode.Self)
            return true;

        RoomGrid grid = _unit.RoomContext != null ? _unit.RoomContext.RoomGrid : null;
        if (grid == null)
        {
            float distance = Vector3.Distance(_unit.Position, primaryTarget.Position);
            return distance <= Mathf.Max(0f, rangeInCells);
        }

        Vector3Int selfCell = ResolveUnitCell(grid, _unit);
        Vector3Int targetCell = ResolveUnitCell(grid, primaryTarget);
        return GridNavigationUtility.IsWithinCellRange(selfCell, targetCell, rangeInCells);
    }

    private static bool ResolveRequiresRangeCheck(SkillData skill)
    {
        return skill != null && skill.Requirements != null && skill.Requirements.requiresTarget;
    }

    private static bool IsSelfCenteredAreaSkill(SkillData skill)
    {
        if (skill == null || skill.TargetMode != SkillTargetMode.Self)
            return false;

        return skill.Shape == SkillShape.Area ||
               skill.Shape == SkillShape.Splash ||
               skill.Shape == SkillShape.MultiTarget;
    }

    private static bool ApplyEffects(SkillData skill, SkillCastContext context, List<Unit> impactedUnits)
    {
        if (skill == null || context == null || impactedUnits == null || impactedUnits.Count == 0)
            return false;

        bool anyApplied = false;
        anyApplied |= ApplySkillEffects(skill, context, impactedUnits);
        anyApplied |= ApplyStatusEffects(skill, context, impactedUnits);
        return anyApplied;
    }

    private static bool ApplySkillEffects(SkillData skill, SkillCastContext context, List<Unit> impactedUnits)
    {
        SkillEffect[] effects = skill.Effects;
        if (effects == null || effects.Length == 0)
            return false;

        bool anyApplied = false;

        for (int i = 0; i < effects.Length; i++)
        {
            SkillEffect effect = effects[i];
            if (effect == null)
                continue;

            for (int targetIndex = 0; targetIndex < impactedUnits.Count; targetIndex++)
            {
                Unit impactedUnit = impactedUnits[targetIndex];
                if (impactedUnit == null)
                    continue;

                anyApplied |= effect.Apply(context, impactedUnit);
            }
        }

        return anyApplied;
    }

    private static bool ApplyStatusEffects(SkillData skill, SkillCastContext context, List<Unit> impactedUnits)
    {
        AppliedStatusEffectSpec[] statusEffects = skill.AppliedStatusEffects;
        if (statusEffects == null || statusEffects.Length == 0)
            return false;

        bool anyApplied = false;

        for (int i = 0; i < statusEffects.Length; i++)
        {
            AppliedStatusEffectSpec spec = statusEffects[i];
            StatusEffectDefinition definition = spec.Definition;
            if (definition == null)
                continue;

            bool requireAlly = spec.RequireAllyTarget;

            for (int targetIndex = 0; targetIndex < impactedUnits.Count; targetIndex++)
            {
                Unit impactedUnit = impactedUnits[targetIndex];
                if (impactedUnit == null || impactedUnit.StatusEffects == null)
                    continue;

                bool isAlly = IsAllyOf(context.Caster, impactedUnit);
                bool showPopupEvenIfBlocked = requireAlly && !isAlly;

                Debug.Log($"[SkillCaster] Applying {definition.DisplayName}. requireAlly={requireAlly}, isAlly={isAlly}, target={impactedUnit.name}");

                StatusEffectApplication application = new(impactedUnit, context.Caster, context.Skill, definition);
                bool applied = impactedUnit.StatusEffects.TryApply(application, showPopupEvenIfBlocked);
                
                Debug.Log($"[SkillCaster] Applied {definition.DisplayName} to {impactedUnit.name}: {applied}");
                
                if (applied)
                    anyApplied = true;
            }
        }

        return anyApplied;
    }

    private static bool IsAllyOf(Unit caster, Unit target)
    {
        return ReferenceEquals(caster, target) || caster.Team == target.Team;
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        return GridUnitCellUtility.ResolveUnitCell(grid, unit);
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

    private static string FormatUnits(List<Unit> units)
    {
        if (units == null || units.Count == 0)
            return "[None]";

        string[] labels = new string[units.Count];
        for (int i = 0; i < units.Count; i++)
            labels[i] = FormatUnitName(units[i]);

        return string.Join(", ", labels);
    }
}

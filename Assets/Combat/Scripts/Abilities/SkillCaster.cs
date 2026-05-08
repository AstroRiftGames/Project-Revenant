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
        if (!CanTryUseSkill(skill))
            return false;

        Unit primaryTarget = ResolvePrimarySkillTarget(skill, combatTarget);
        if (!TryValidatePrimarySkillTarget(skill, primaryTarget))
            return false;

        if (!IsSkillTargetInRange(skill, primaryTarget))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(primaryTarget)} is out of range.");
            return false;
        }

        SkillCastContext context = BuildSkillCastContext(skill, primaryTarget);
        if (!CollectSkillTargets(context))
            return false;

        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} '{skill.DisplayName}' resolved primary target {FormatUnitName(primaryTarget)} " +
            $"and {_impactedUnitsBuffer.Count} impacted unit(s): {FormatUnits(_impactedUnitsBuffer)}.");

        if (!ApplySkillEffects(skill, context))
            return false;

        OnSkillCastSucceeded(skill, context, primaryTarget);
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

    private Unit ResolvePrimarySkillTarget(SkillData skill, Unit combatTarget)
    {
        if (skill == null || _unit == null)
            return null;

        if (ShouldResolveSelfAsPrimaryTarget(skill))
            return _unit.IsAlive ? _unit : null;

        if (IsPrimarySkillTargetValid(skill, combatTarget))
            return combatTarget;

        return FindFallbackTarget(skill);
    }

    private bool TryValidatePrimarySkillTarget(SkillData skill, Unit primaryTarget)
    {
        if (primaryTarget == null)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' resolved no target for mode {skill.TargetMode}.");
            return false;
        }

        if (IsPrimarySkillTargetValid(skill, primaryTarget))
            return true;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(primaryTarget)} failed requirements.");
        return false;
    }

    private bool IsPrimarySkillTargetValid(SkillData skill, Unit primaryTarget)
    {
        if (skill == null)
            return false;

        if (IsSelfCenteredAreaSkill(skill))
            return _unit != null && _unit.IsAlive;

        return UnitTargetValidator.IsTargetSelectableForSkill(_unit, primaryTarget, skill.Requirements);
    }

    private bool IsSkillTargetInRange(SkillData skill, Unit primaryTarget)
    {
        if (_unit == null || skill == null)
            return false;

        if (primaryTarget == null)
            return !ResolveRequiresRangeCheck(skill);

        int rangeInCells = skill.RangeInCells;
        if (skill.TargetMode == SkillTargetMode.Self)
            return true;

        return UnitTargetValidator.IsTargetInRange(_unit, primaryTarget, rangeInCells);
    }

    private SkillCastContext BuildSkillCastContext(SkillData skill, Unit primaryTarget)
    {
        return new SkillCastContext(_unit, skill, primaryTarget);
    }

    private bool CollectSkillTargets(SkillCastContext context)
    {
        _impactedUnitsBuffer.Clear();
        if (SkillTargetCollector.TryCollectTargets(context, _impactedUnitsBuffer, LogDebug))
            return true;

        SkillData skill = context != null ? context.Skill : null;
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill?.DisplayName}' shape '{skill?.Shape}' produced no impacted targets.");
        return false;
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

    private static bool ShouldResolveSelfAsPrimaryTarget(SkillData skill)
    {
        if (skill == null)
            return false;

        if (skill.TargetMode == SkillTargetMode.Self)
            return true;

        return UnitTargetValidator.ResolveSkillTargetRequirement(skill.Requirements) == SkillTargetRequirement.Self;
    }

    private bool ApplySkillEffects(SkillData skill, SkillCastContext context)
    {
        if (ApplyResolvedEffects(skill, context, _impactedUnitsBuffer))
            return true;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' applied no effects to impacted targets.");
        return false;
    }

    private void OnSkillCastSucceeded(SkillData skill, SkillCastContext context, Unit primaryTarget)
    {
        BreakInvisibilityAfterSkillUse();
        ConsumeSkillCooldown(skill);
        NotifySkillUsed(skill, context, primaryTarget);
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

    private void NotifySkillUsed(SkillData skill, SkillCastContext context, Unit primaryTarget)
    {
        int listenerCount = SkillUsed?.GetInvocationList().Length ?? 0;
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} emitting SkillUsed for '{skill.DisplayName}' with {listenerCount} listener(s).");
        SkillUsed?.Invoke(context, primaryTarget);
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} used '{skill.DisplayName}' on {_impactedUnitsBuffer.Count} impacted unit(s). Primary target: {FormatUnitName(primaryTarget)}.");
    }

    private static bool ApplyResolvedEffects(SkillData skill, SkillCastContext context, List<Unit> impactedUnits)
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

                StatusEffectApplication application = new(impactedUnit, context.Caster, context.Skill, definition);
                bool applied = impactedUnit.StatusEffects.TryApply(application, showPopupEvenIfBlocked);
                
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

    private Unit FindFallbackTarget(SkillData skill)
    {
        if (_unit == null || skill == null)
            return null;

        RequiredTargetRelationship relationship = ResolveFallbackTargetRelationship(skill);
        return relationship switch
        {
            RequiredTargetRelationship.Ally => TargetSelectionUtility.SelectLowestHealthRatioTarget(
                _unit,
                _unit.GetRoomUnits(),
                candidate => IsPrimarySkillTargetValid(skill, candidate)),
            RequiredTargetRelationship.Hostile => TargetSelectionUtility.SelectClosestTarget(
                _unit,
                _unit.GetRoomUnits(),
                candidate => IsPrimarySkillTargetValid(skill, candidate)),
            _ => null
        };
    }

    private static RequiredTargetRelationship ResolveFallbackTargetRelationship(SkillData skill)
    {
        SkillRequirements requirements = skill != null ? skill.Requirements : null;
        return UnitTargetValidator.ResolveSkillRelationship(requirements);
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

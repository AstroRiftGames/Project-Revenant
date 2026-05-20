using System;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityChargeSource
{
    BasicAttack,
    BasicHeal,
    Kill,
    DamageTaken,
    NearbyAllySkillUsed
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
public class SkillCaster : MonoBehaviour
{
    public static event Action<Unit, SkillData, Unit> AnySkillUsed;

    [SerializeField] private SkillData _overrideSkill;
    [Header("Transition - Ability Charge")]
    [SerializeField] private bool _useAbilityChargeReadiness = true;
    [SerializeField] private bool _allowLegacyCooldownFallback = false;
    [SerializeField] private float _maxAbilityCharge = 100f;
    [SerializeField] private float _chargePerSuccessfulBasicAttack = 25f;
    [SerializeField] private float _chargePerSuccessfulBasicHeal = 25f;
    [SerializeField] private float _chargeFromKill = 20f;
    [SerializeField] private float _chargeFromDamageTaken = 10f;
    [SerializeField] private float _chargeFromNearbyAllySkillUsed = 15f;
    [SerializeField] private float _nearbyAllySkillChargeRadius = 3.5f;
    [SerializeField] private bool _debugLogs;

    private Unit _unit;
    private LifeController _lifeController;
    private UnitMovement _movement;
    private SkillData _resolvedSkill;
    private readonly SkillState _state = new();
    private readonly List<Unit> _unitsHit = new();
    private SkillData _castingSkill;
    private SkillContext _castingContext;
    private Unit _castingTarget;
    private float _castRemainingTime;

    public event Action<Unit, SkillData, Unit> SkillUsed;

    public SkillData Skill => ResolveSkill();
    public bool HasSkill => Skill != null;
    public float CurrentCooldown => _state.RemainingCooldown;
    public float MaxCooldown => Skill != null ? Skill.Cooldown : 0f;
    public float CurrentCharge => _state.CurrentCharge;
    public float MaxCharge => _state.MaxCharge;
    public bool IsCasting => _castingSkill != null;
    public SkillData CurrentCastingSkill => _castingSkill;
    public Unit CastTarget => _castingContext != null ? _castingContext.PrimaryTarget : _castingTarget;
    public float CastRemainingTime => Mathf.Max(0f, _castRemainingTime);
    public bool IsSkillReady => !IsCasting && ResolveSkillReadiness(Skill);
    public bool UsesAbilityChargeVisual => HasSkill && _useAbilityChargeReadiness;
    public bool UsesLegacyCooldownReadiness => HasSkill && !_useAbilityChargeReadiness && _allowLegacyCooldownFallback;
    public Sprite Icon => Skill != null ? Skill.Icon : null;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _lifeController = GetComponent<LifeController>();
        _movement = GetComponent<UnitMovement>();
        _state.ConfigureCharge(_maxAbilityCharge);
        ResolveSkill();
    }

    private void OnEnable()
    {
        LifeController.OnUnitDied += HandleUnitDied;
        AnySkillUsed += HandleAnySkillUsed;

        if (_lifeController != null)
            _lifeController.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        CancelCurrentCast();
        LifeController.OnUnitDied -= HandleUnitDied;
        AnySkillUsed -= HandleAnySkillUsed;

        if (_lifeController != null)
            _lifeController.OnDamageTaken -= HandleDamageTaken;
    }

    private void Update()
    {
        UpdateCasting();

        if (_state.IsReady || !CanTickLegacyCooldown())
            return;

        _state.Tick(Time.deltaTime);
    }

    public bool TryUse(Unit combatTarget)
    {
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} attempting skill. Combat target: {FormatUnitName(combatTarget)}.");

        SkillData skill = ResolveSkill();
        if (!CanStartCast(skill))
            return false;

        if (!TryBuildSkillContext(skill, combatTarget, out SkillContext skillContext))
            return false;

        if (!TryValidateSkillContext(skillContext))
            return false;

        if (!IsSkillContextInRange(skillContext))
        {
            LogDebug(
                $"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatSkillContext(skillContext)} is out of range.");
            return false;
        }

        if (!BeginCast(skillContext))
            return false;

        if (!ShouldCompleteCastImmediately(skill))
            return true;

        return CompleteCast();
    }

    public void ResetState()
    {
        _state.Reset();
    }

    public void NotifyBasicActionHit()
    {
        AddAbilityChargeFromSource(AbilityChargeSource.BasicAttack);
    }

    public void NotifyBasicActionSucceeded(TargetRelation targetRelation)
    {
        AbilityChargeSource source = ResolveBasicActionChargeSource(targetRelation);
        if (source == AbilityChargeSource.BasicHeal && !CanChargeFromBasicHeal())
            return;

        AddAbilityChargeFromSource(source);
    }

    public void AddAbilityChargeFromSource(AbilityChargeSource source)
    {
        if (!CanReceiveChargeFromSource(source))
            return;

        AddAbilityCharge(GetChargeAmountForSource(source), source);
    }

    public void AddAbilityCharge(float amount)
    {
        AddAbilityCharge(amount, AbilityChargeSource.BasicAttack);
    }

    public void AddAbilityCharge(float amount, AbilityChargeSource source)
    {
        if (!CanApplyAbilityCharge(source))
            return;

        if (!CanGainAbilityCharge())
            return;

        float previousCharge = _state.CurrentCharge;
        _state.AddCharge(amount);

        if (_state.CurrentCharge > previousCharge)
        {
            LogDebug(
                $"[SkillCaster] {FormatOwnerIdentity()} gained ability charge from {source}: " +
                $"{_state.CurrentCharge:F1}/{_state.MaxCharge:F1}.");
        }
    }

    public void ResetAbilityCharge()
    {
        _state.ResetCharge();
    }

    public void InterruptCast()
    {
        InterruptCast("external interruption");
    }

    private bool CanTickLegacyCooldown()
    {
        return _unit == null || _unit.StatusEffects == null || !_unit.StatusEffects.PreventsSkillCooldownCharge;
    }

    private bool CanGainAbilityCharge()
    {
        return _unit == null || _unit.StatusEffects == null || !_unit.StatusEffects.PreventsSkillCooldownCharge;
    }

    private bool CanChargeFromBasicHeal()
    {
        if (_unit == null)
            return false;

        IBasicAction action = _unit.Action;
        return action != null &&
               action.TargetRelation == TargetRelation.Ally &&
               action.RequiresInjuredTarget;
    }

    private static AbilityChargeSource ResolveBasicActionChargeSource(TargetRelation targetRelation)
    {
        return targetRelation == TargetRelation.Ally
            ? AbilityChargeSource.BasicHeal
            : AbilityChargeSource.BasicAttack;
    }

    private float GetChargeAmountForSource(AbilityChargeSource source)
    {
        return source switch
        {
            AbilityChargeSource.BasicHeal => Mathf.Max(0f, _chargePerSuccessfulBasicHeal),
            AbilityChargeSource.BasicAttack => Mathf.Max(0f, _chargePerSuccessfulBasicAttack),
            AbilityChargeSource.Kill => Mathf.Max(0f, _chargeFromKill),
            AbilityChargeSource.DamageTaken => Mathf.Max(0f, _chargeFromDamageTaken),
            AbilityChargeSource.NearbyAllySkillUsed => Mathf.Max(0f, _chargeFromNearbyAllySkillUsed),
            _ => 0f
        };
    }

    private bool CanReceiveChargeFromSource(AbilityChargeSource source)
    {
        if (_unit == null)
            return false;

        if (!CanApplyAbilityCharge(source))
            return false;

        return source switch
        {
            AbilityChargeSource.Kill => _unit.Role == UnitRole.DPS,
            AbilityChargeSource.DamageTaken => _unit.Role == UnitRole.Tank,
            AbilityChargeSource.NearbyAllySkillUsed => _unit.Role == UnitRole.Support,
            _ => true
        };
    }

    private void HandleUnitDied(Unit deadUnit)
    {
        if (_unit == null || deadUnit == null || _unit.Role != UnitRole.DPS || !_unit.IsAlive)
            return;

        Unit killer = deadUnit.GetLastAttacker();
        if (!ReferenceEquals(killer, _unit))
            return;

        if (ReferenceEquals(deadUnit, _unit) || deadUnit.Team == _unit.Team)
            return;

        AddAbilityChargeFromSource(AbilityChargeSource.Kill);
    }

    private void HandleDamageTaken(int amount)
    {
        if (_unit == null || _unit.Role != UnitRole.Tank || amount <= 0)
            return;

        AddAbilityChargeFromSource(AbilityChargeSource.DamageTaken);
    }

    private void HandleAnySkillUsed(Unit caster, SkillData skill, Unit popupAnchor)
    {
        if (_unit == null || _unit.Role != UnitRole.Support || !_unit.IsAlive)
            return;

        if (caster == null || skill == null || ReferenceEquals(caster, _unit))
            return;

        if (caster.Team != _unit.Team)
            return;

        if (!ReferenceEquals(caster.RoomContext, _unit.RoomContext))
            return;

        if (!IsWithinNearbyAllySkillChargeRange(caster))
            return;

        AddAbilityChargeFromSource(AbilityChargeSource.NearbyAllySkillUsed);
    }

    private bool IsWithinNearbyAllySkillChargeRange(Unit caster)
    {
        if (_unit == null || caster == null)
            return false;

        float chargeRadius = Mathf.Max(0f, _nearbyAllySkillChargeRadius);
        if (chargeRadius <= 0f)
            return false;

        float sqrDistance = (_unit.Position - caster.Position).sqrMagnitude;
        return sqrDistance <= chargeRadius * chargeRadius;
    }

    private bool CanStartCast(SkillData skill)
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

        if (IsCasting)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: already casting '{_castingSkill.DisplayName}'.");
            return false;
        }

        if (!_unit.IsAlive)
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: owner is dead.");
            return false;
        }

        if (_unit.StatusEffects != null && (!_unit.StatusEffects.CanAct || !_unit.StatusEffects.CanUseSkills))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: active status effect blocks skill usage.");
            return false;
        }

        if (ResolveSkillReadiness(skill))
            return true;

        if (UsesAbilityChargeReadiness(skill))
        {
            float missingCharge = Mathf.Max(0f, _state.MaxCharge - _state.CurrentCharge);
            LogDebug(
                $"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' is not charged " +
                $"({_state.CurrentCharge:F1}/{_state.MaxCharge:F1}, missing {missingCharge:F1}).");
            return false;
        }

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' is on cooldown for {_state.RemainingCooldown:F2}s.");
        return false;
    }

    private bool BeginCast(SkillContext skillContext)
    {
        SkillData skill = skillContext != null ? skillContext.Skill : null;
        if (skill == null || _unit == null || IsCasting)
            return false;

        _castingSkill = skill;
        _castingContext = skillContext;
        _castingTarget = skillContext.PrimaryTarget;
        _castRemainingTime = skill.CastTime;
        _movement?.InterruptMovement();

        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} began cast for '{skill.DisplayName}' " +
            $"with context {FormatSkillContext(skillContext)}. Cast time: {skill.CastTime:F2}s.");
        return true;
    }

    private bool CompleteCast()
    {
        SkillData skill = _castingSkill;
        if (skill == null)
            return false;

        if (ShouldInterruptCurrentCast())
        {
            InterruptCast("owner can no longer complete cast");
            return false;
        }

        if (!TryResolveCastContext(skill, out SkillContext resolvedContext))
        {
            CancelCurrentCast();
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} canceled '{skill.DisplayName}' because no valid skill context remained.");
            return false;
        }

        if (!TryCollectUnitsHit(resolvedContext))
        {
            CancelCurrentCast();
            return false;
        }

        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} '{skill.DisplayName}' resolved context {FormatSkillContext(resolvedContext)} " +
            $"and {_unitsHit.Count} target(s) hit: {FormatUnits(_unitsHit)}.");

        if (!ApplySkillToUnitsHit(resolvedContext))
        {
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' applied no effects to impacted targets.");
            CancelCurrentCast();
            return false;
        }

        ClearCastingState();
        OnSkillCastSucceeded(skill, resolvedContext.PrimaryTarget);
        return true;
    }

    private void UpdateCasting()
    {
        if (!IsCasting)
            return;

        if (ShouldInterruptCurrentCast())
        {
            InterruptCast("owner became unable to act during casting");
            return;
        }

        if (_castRemainingTime <= 0f)
            return;

        _castRemainingTime = Mathf.Max(0f, _castRemainingTime - Mathf.Max(0f, Time.deltaTime));
        if (_castRemainingTime > 0f)
            return;

        CompleteCast();
    }

    private bool ShouldCompleteCastImmediately(SkillData skill)
    {
        return skill == null || skill.CastTime <= 0f;
    }

    private bool TryResolveCastContext(SkillData skill, out SkillContext resolvedContext)
    {
        resolvedContext = _castingContext;
        if (CanCompleteCastWithContext(resolvedContext))
            return true;

        Unit previousPrimaryTarget = resolvedContext != null ? resolvedContext.PrimaryTarget : _castingTarget;
        if (!TryBuildSkillContext(skill, previousPrimaryTarget, out SkillContext rebuiltContext))
            return false;

        if (!CanCompleteCastWithContext(rebuiltContext))
            return false;

        if (SkillContextsMatch(resolvedContext, rebuiltContext))
            return false;

        _castingContext = rebuiltContext;
        _castingTarget = rebuiltContext.PrimaryTarget;
        resolvedContext = rebuiltContext;
        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} rebuilt '{skill.DisplayName}' context to {FormatSkillContext(rebuiltContext)} before completion.");
        return true;
    }

    private bool CanCompleteCastWithContext(SkillContext skillContext)
    {
        return TryValidateSkillContext(skillContext, false) && IsSkillContextInRange(skillContext);
    }

    private bool ShouldInterruptCurrentCast()
    {
        if (_unit == null || !_unit.IsAlive)
            return true;

        StatusEffectController statusEffects = _unit.StatusEffects;
        if (statusEffects == null)
            return false;

        return !statusEffects.CanAct || !statusEffects.CanUseSkills;
    }

    private void InterruptCast(string reason)
    {
        if (!IsCasting)
            return;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} interrupted '{_castingSkill.DisplayName}': {reason}.");
        ClearCastingState();
    }

    private void CancelCurrentCast()
    {
        if (!IsCasting)
            return;

        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} canceled '{_castingSkill.DisplayName}' before completion.");
        ClearCastingState();
    }

    private void ClearCastingState()
    {
        _castingSkill = null;
        _castingContext = null;
        _castingTarget = null;
        _castRemainingTime = 0f;
    }

    private bool ResolveSkillReadiness(SkillData skill)
    {
        if (skill == null)
            return false;

        if (UsesAbilityChargeReadiness(skill))
            return _state.IsChargeReady;

        if (UsesLegacyCooldownCompatibility(skill))
            return _state.IsCooldownReady;

        return false;
    }

    private bool UsesAbilityChargeReadiness(SkillData skill)
    {
        return skill != null && _useAbilityChargeReadiness;
    }

    private bool UsesLegacyCooldownCompatibility(SkillData skill)
    {
        return skill != null && !_useAbilityChargeReadiness && _allowLegacyCooldownFallback;
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

        SkillTargetRequirement targetRequirement = ResolveTargetRequirement(skill);
        if (targetRequirement == SkillTargetRequirement.Self)
            return _unit.IsAlive ? _unit : null;

        if (targetRequirement == SkillTargetRequirement.NoTarget ||
            targetRequirement == SkillTargetRequirement.GroundCell)
        {
            return null;
        }

        if (ShouldUseAllyTargetSelection(skill))
            return SelectPreferredAllySkillTarget(skill, combatTarget);

        if (ShouldUseOffensiveTargetSelection(skill))
            return SelectPreferredOffensiveSkillTarget(skill, combatTarget);

        if (CanChooseTarget(skill, combatTarget))
            return combatTarget;

        if (AllowsNullPrimaryTarget(skill))
            return null;

        return FindFallbackTarget(skill);
    }

    private bool TryBuildSkillContext(SkillData skill, Unit combatTarget, out SkillContext skillContext)
    {
        skillContext = null;
        if (skill == null || _unit == null)
            return false;

        Unit primaryTarget = ChooseSkillTarget(skill, combatTarget);
        skillContext = BuildSkillContext(skill, primaryTarget);
        return skillContext != null;
    }

    private SkillContext BuildSkillContext(SkillData skill, Unit primaryTarget)
    {
        if (skill == null || _unit == null)
            return null;

        RoomContext roomContext = _unit.RoomContext;
        RoomGrid roomGrid = roomContext != null ? roomContext.RoomGrid : null;
        Vector2Int targetCell = default;
        bool hasTargetCell = false;
        Unit impactCenterUnit = ResolveImpactCenterUnit(skill, primaryTarget);
        Vector3 impactCenterWorld = ResolveImpactCenterWorld(roomGrid, impactCenterUnit, hasTargetCell, targetCell);

        return new SkillContext(
            _unit,
            skill,
            primaryTarget,
            targetCell,
            hasTargetCell,
            impactCenterWorld,
            impactCenterUnit,
            roomContext,
            roomGrid);
    }

    private Unit ResolveImpactCenterUnit(SkillData skill, Unit primaryTarget)
    {
        if (skill == null || _unit == null)
            return null;

        if (skill.UsesCasterAsImpactCenter)
            return _unit;

        if (primaryTarget != null)
            return primaryTarget;

        SkillTargetRequirement targetRequirement = ResolveTargetRequirement(skill);
        if (targetRequirement == SkillTargetRequirement.Self)
            return _unit;

        return null;
    }

    private Vector3 ResolveImpactCenterWorld(RoomGrid roomGrid, Unit impactCenterUnit, bool hasTargetCell, Vector2Int targetCell)
    {
        if (impactCenterUnit != null)
            return impactCenterUnit.Position;

        if (hasTargetCell && roomGrid != null)
            return roomGrid.CellToWorld(new Vector3Int(targetCell.x, targetCell.y, 0));

        return _unit != null ? _unit.Position : Vector3.zero;
    }

    private bool TryValidateChosenTarget(SkillData skill, Unit selectedTarget)
    {
        SkillContext skillContext = BuildSkillContext(skill, selectedTarget);
        return TryValidateSkillContext(skillContext);
    }

    private bool TryValidateSkillContext(SkillContext skillContext)
    {
        return TryValidateSkillContext(skillContext, true);
    }

    private bool TryValidateSkillContext(SkillContext skillContext, bool logFailure)
    {
        if (skillContext == null || skillContext.Skill == null || skillContext.Caster == null)
        {
            if (logFailure)
                LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: skill context could not be built.");
            return false;
        }

        SkillData skill = skillContext.Skill;
        SkillTargetRequirement targetRequirement = ResolveTargetRequirement(skill);

        if (RequiresUnitPrimaryTarget(skill))
        {
            if (!skillContext.HasPrimaryTarget)
            {
                if (logFailure)
                    LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' resolved no valid primary target.");
                return false;
            }

            if (CanChooseTarget(skill, skillContext.PrimaryTarget))
                return HasValidImpactCenter(skillContext, logFailure);

            if (logFailure)
                LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' target {FormatUnitName(skillContext.PrimaryTarget)} failed skill rules.");
            return false;
        }

        if (skillContext.HasPrimaryTarget)
        {
            if (logFailure)
                LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' should not carry a primary unit target.");
            return false;
        }

        if (targetRequirement == SkillTargetRequirement.GroundCell && !skillContext.HasTargetCell)
        {
            if (logFailure)
                LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill.DisplayName}' requires a ground cell, but no target cell flow exists yet.");
            return false;
        }

        return HasValidImpactCenter(skillContext, logFailure);
    }

    private bool HasValidImpactCenter(SkillContext skillContext, bool logFailure)
    {
        if (skillContext == null)
            return false;

        if (skillContext.HasImpactCenterUnit || skillContext.HasTargetCell)
            return true;

        if (logFailure)
        {
            SkillData skill = skillContext.Skill;
            LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill?.DisplayName ?? "Unknown"}' resolved no valid impact center.");
        }

        return false;
    }

    private bool CanChooseTarget(SkillData skill, Unit target)
    {
        if (skill == null || _unit == null)
            return false;

        if (target == null)
            return AllowsNullPrimaryTarget(skill);

        if (!RequiresUnitPrimaryTarget(skill))
            return false;

        if (!UnitTargetValidator.IsSkillTargetSelectable(_unit, target, skill.Requirements))
            return false;

        return skill.Requirements == null || skill.Requirements.AreSkillSpecificRequirementsMet(_unit, target);
    }

    private bool ShouldUseAllyTargetSelection(SkillData skill)
    {
        if (skill == null)
            return false;

        SkillRequirements requirements = skill.Requirements;
        return requirements != null && requirements.TargetRequirement == SkillTargetRequirement.Ally;
    }

    private bool ShouldUseOffensiveTargetSelection(SkillData skill)
    {
        if (skill == null)
            return false;

        SkillRequirements requirements = skill.Requirements;
        return requirements != null && requirements.TargetRequirement == SkillTargetRequirement.Hostile;
    }

    private Unit SelectPreferredAllySkillTarget(SkillData skill, Unit currentTarget)
    {
        if (skill == null || _unit == null)
            return null;

        IReadOnlyList<Unit> roomUnits = _unit.GetRoomUnits();
        Func<Unit, bool> canChooseTarget = candidate => CanChooseTarget(skill, candidate);

        if (IsHealingAllySkill(skill))
            return TargetingStrategy.SelectBestHealingAllyTarget(_unit, currentTarget, roomUnits, canChooseTarget);

        return TargetingStrategy.SelectBestBuffAllyTarget(_unit, currentTarget, roomUnits, canChooseTarget);
    }

    private Unit SelectPreferredOffensiveSkillTarget(SkillData skill, Unit currentTarget)
    {
        if (skill == null || _unit == null)
            return null;

        IReadOnlyList<Unit> roomUnits = _unit.GetRoomUnits();
        Func<Unit, bool> canChooseTarget = candidate => CanChooseTarget(skill, candidate);
        return TargetingStrategy.SelectBestOffensiveTarget(_unit, currentTarget, roomUnits, canChooseTarget);
    }

    private bool IsHealingAllySkill(SkillData skill)
    {
        if (skill == null)
            return false;

        SkillRequirements requirements = skill.Requirements;
        if (requirements != null && requirements.mustTargetInjured)
            return true;

        SkillEffect[] effects = skill.Effects;
        if (effects != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] is HealSkillEffect)
                    return true;
            }
        }

        SkillStatusEffect[] statusEffects = skill.StatusEffects;
        if (statusEffects != null)
        {
            for (int i = 0; i < statusEffects.Length; i++)
            {
                StatusEffectDefinition definition = statusEffects[i].Definition;
                if (definition != null && definition.IsHeal)
                    return true;
            }
        }

        return false;
    }

    private bool IsTargetCloseEnough(SkillData skill, Unit selectedTarget)
    {
        SkillContext skillContext = BuildSkillContext(skill, selectedTarget);
        return IsSkillContextInRange(skillContext);
    }

    private bool IsSkillContextInRange(SkillContext skillContext)
    {
        if (skillContext == null || skillContext.Caster == null || skillContext.Skill == null)
            return false;

        if (skillContext.HasPrimaryTarget)
            return UnitTargetValidator.IsSkillTargetInRange(skillContext.Caster, skillContext.PrimaryTarget, skillContext.Skill);

        if (skillContext.HasTargetCell)
            return IsTargetCellInRange(skillContext);

        return AllowsNullPrimaryTarget(skillContext.Skill);
    }

    private bool IsTargetCellInRange(SkillContext skillContext)
    {
        if (skillContext == null || skillContext.Caster == null || skillContext.Skill == null || !skillContext.HasTargetCell)
            return false;

        Unit caster = skillContext.Caster;
        SkillData skill = skillContext.Skill;
        RoomGrid roomGrid = skillContext.RoomGrid;
        if (roomGrid == null)
        {
            float distance = Vector3.Distance(caster.Position, skillContext.ImpactCenterWorld);
            return distance <= Mathf.Max(0f, skill.RangeInCells);
        }

        Vector3Int casterCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, caster);
        Vector3Int targetCell = new(skillContext.TargetCell.x, skillContext.TargetCell.y, 0);
        return GridNavigationUtility.IsWithinCellRange(casterCell, targetCell, skill.RangeInCells);
    }

    private bool TryCollectUnitsHit(SkillContext skillContext)
    {
        _unitsHit.Clear();
        if (SkillHitCollector.TryCollectTargets(skillContext, _unitsHit, LogDebug))
            return true;

        SkillData skill = skillContext != null ? skillContext.Skill : null;
        LogDebug($"[SkillCaster] {FormatOwnerIdentity()} aborted: '{skill?.DisplayName ?? "Unknown"}' shape '{skill?.Shape}' produced no targets.");
        return false;
    }

    private bool ApplySkillToUnitsHit(SkillContext skillContext)
    {
        SkillData skill = skillContext != null ? skillContext.Skill : null;
        if (skill == null || _unit == null || _unitsHit.Count == 0)
            return false;

        bool anyApplied = false;
        for (int targetIndex = 0; targetIndex < _unitsHit.Count; targetIndex++)
        {
            Unit hitUnit = _unitsHit[targetIndex];
            if (hitUnit == null)
                continue;

            anyApplied |= ApplySkillToUnit(skillContext, hitUnit);
        }

        return anyApplied;
    }

    private bool ApplySkillToUnit(SkillContext skillContext, Unit hitUnit)
    {
        SkillData skill = skillContext != null ? skillContext.Skill : null;
        if (skill == null || hitUnit == null)
            return false;

        bool anyApplied = false;

        anyApplied |= ApplySkillEffectsToUnit(skillContext, hitUnit);
        anyApplied |= ApplyStatusEffectsToUnit(skillContext, hitUnit);
        return anyApplied;
    }

    private bool ApplySkillEffectsToUnit(SkillContext skillContext, Unit hitUnit)
    {
        SkillData skill = skillContext != null ? skillContext.Skill : null;
        SkillEffect[] effects = skill.Effects;
        if (effects == null || effects.Length == 0)
            return false;

        bool anyApplied = false;
        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
        {
            SkillEffect effect = effects[effectIndex];
            if (effect == null)
                continue;

            anyApplied |= effect.Apply(skillContext, hitUnit);
        }

        return anyApplied;
    }

    private bool ApplyStatusEffectsToUnit(SkillContext skillContext, Unit hitUnit)
    {
        SkillData skill = skillContext != null ? skillContext.Skill : null;
        if (skill == null)
            return false;

        SkillStatusEffect[] statusEffects = skill.StatusEffects;
        if (statusEffects == null || statusEffects.Length == 0 || hitUnit == null || hitUnit.StatusEffects == null)
            return false;

        bool anyApplied = false;
        for (int effectIndex = 0; effectIndex < statusEffects.Length; effectIndex++)
        {
            SkillStatusEffect statusEffect = statusEffects[effectIndex];
            StatusEffectDefinition definition = statusEffect.Definition;
            if (definition == null || !CanApplyStatusToUnit(skillContext, statusEffect.TargetRelation, hitUnit))
                continue;

            StatusEffectApplication application = BuildStatusEffectApplication(skillContext, hitUnit, definition);
            anyApplied |= hitUnit.StatusEffects.TryApply(application);
        }

        return anyApplied;
    }

    private StatusEffectApplication BuildStatusEffectApplication(
        SkillContext skillContext,
        Unit hitUnit,
        StatusEffectDefinition definition)
    {
        return new(
            hitUnit,
            skillContext != null ? skillContext.Caster : null,
            skillContext != null ? skillContext.Skill : null,
            definition);
    }

    private bool CanApplyStatusToUnit(SkillContext skillContext, TargetRelation targetRelation, Unit hitUnit)
    {
        Unit caster = skillContext != null ? skillContext.Caster : null;
        if (caster == null || hitUnit == null)
            return false;

        return targetRelation switch
        {
            TargetRelation.Hostile => caster.IsHostileTo(hitUnit),
            TargetRelation.Ally => !caster.IsHostileTo(hitUnit),
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
            SkillTargetRequirement.Ally => SelectPreferredAllySkillTarget(skill, null),
            SkillTargetRequirement.Hostile => SelectPreferredOffensiveSkillTarget(skill, null),
            SkillTargetRequirement.NoTarget => null,
            SkillTargetRequirement.GroundCell => null,
            _ => FindClosestTarget(skill)
        };
    }

    private static bool RequiresUnitPrimaryTarget(SkillData skill)
    {
        SkillTargetRequirement targetRequirement = ResolveTargetRequirement(skill);
        return targetRequirement != SkillTargetRequirement.NoTarget &&
               targetRequirement != SkillTargetRequirement.GroundCell;
    }

    private static bool AllowsNullPrimaryTarget(SkillData skill)
    {
        SkillTargetRequirement targetRequirement = ResolveTargetRequirement(skill);
        return targetRequirement == SkillTargetRequirement.NoTarget ||
               targetRequirement == SkillTargetRequirement.GroundCell;
    }

    private static SkillTargetRequirement ResolveTargetRequirement(SkillData skill)
    {
        SkillRequirements requirements = skill != null ? skill.Requirements : null;
        return requirements != null
            ? requirements.TargetRequirement
            : SkillTargetRequirement.Any;
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

    private bool CanApplyAbilityCharge(AbilityChargeSource source)
    {
        if (_unit == null)
            return false;

        UnitLifecycleState lifecycleState = _unit.LifecycleState;
        if (lifecycleState == UnitLifecycleState.Removed ||
            lifecycleState == UnitLifecycleState.Recruitable ||
            lifecycleState == UnitLifecycleState.Dead)
        {
            return false;
        }

        if (source == AbilityChargeSource.DamageTaken)
            return true;

        return _unit.IsAlive;
    }

    private void OnSkillCastSucceeded(SkillData skill, Unit selectedTarget)
    {
        ConsumeChargeOnSuccess(skill);
        BreakInvisibilityAfterSkillUse();
        NotifySkillUsed(skill, ResolvePopupAnchor(skill, selectedTarget));
    }

    private void BreakInvisibilityAfterSkillUse()
    {
        if (_unit != null && _unit.StatusEffects != null && _unit.StatusEffects.HasInvisibility)
            _unit.StatusEffects.RemoveEffectOfType(StatusEffectType.Invisibility);
    }

    private void ConsumeChargeOnSuccess(SkillData skill)
    {
        if (skill == null)
            return;

        ResetAbilityCharge();
        _state.StartCooldown(skill.Cooldown);
        LogDebug(
            $"[SkillCaster] {FormatOwnerIdentity()} consumed '{skill.DisplayName}' availability. " +
            $"Charge reset to {_state.CurrentCharge:F1}/{_state.MaxCharge:F1}; cooldown {skill.Cooldown:F2}s.");
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
        AnySkillUsed?.Invoke(_unit, skill, popupAnchor);
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

    private static bool SkillContextsMatch(SkillContext left, SkillContext right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        return ReferenceEquals(left.Caster, right.Caster) &&
               ReferenceEquals(left.Skill, right.Skill) &&
               ReferenceEquals(left.PrimaryTarget, right.PrimaryTarget) &&
               left.HasTargetCell == right.HasTargetCell &&
               (!left.HasTargetCell || left.TargetCell == right.TargetCell) &&
               ReferenceEquals(left.ImpactCenterUnit, right.ImpactCenterUnit);
    }

    private static string FormatSkillContext(SkillContext skillContext)
    {
        if (skillContext == null)
            return "[NoContext]";

        string primaryTarget = skillContext.HasPrimaryTarget
            ? FormatUnitName(skillContext.PrimaryTarget)
            : "[None]";
        string impactCenterUnit = skillContext.HasImpactCenterUnit
            ? FormatUnitName(skillContext.ImpactCenterUnit)
            : "[None]";
        string targetCell = skillContext.HasTargetCell
            ? $"({skillContext.TargetCell.x}, {skillContext.TargetCell.y})"
            : "[None]";

        return $"Primary={primaryTarget}, ImpactUnit={impactCenterUnit}, TargetCell={targetCell}, ImpactWorld=({skillContext.ImpactCenterWorld.x:F2}, {skillContext.ImpactCenterWorld.y:F2}, {skillContext.ImpactCenterWorld.z:F2})";
    }
}

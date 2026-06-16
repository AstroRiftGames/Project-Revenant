using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
public class StatusEffectController : MonoBehaviour
{
    [SerializeField] private bool _debugLogs;

    private Unit _unit;
    private LifeController _lifeController;
    private UnitMovement _unitMovement;
    private SkillCaster _skillCaster;
    private readonly List<ActiveStatusEffect> _activeEffects = new();
    private bool _runtimeStoppedByDeath;

    public event Action<StatusEffectController, ActiveStatusEffect> EffectApplied;
    public event Action<StatusEffectController, ActiveStatusEffect> EffectRefreshed;
    public event Action<StatusEffectController, ActiveStatusEffect> EffectStackChanged;
    public event Action<StatusEffectController, ActiveStatusEffect, int> EffectTickResolved;
    public event Action<StatusEffectController, ActiveStatusEffect, StatusEffectRemovalReason> EffectRemoved;

    public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;
    public bool HasStun => HasEffect(SkillEffectKind.Stun);
    public bool HasKnockback => HasEffect(SkillEffectKind.Knockback);
    public bool CanAct => !HasBlockingActionEffect();
    public bool CanMove => !HasMovementRestriction();
    public bool CanAttack => !HasStun;
    public bool CanUseSkills => !HasStun;
    public bool CanMoveTowardTarget => !HasStun;
    public bool ShouldFlee => false;
    public bool RestrictsMovement => HasMovementRestriction();
    public bool PreventsSkillCharge => false;
    public bool IsImmuneToControl => false;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _lifeController = GetComponent<LifeController>();
        _unitMovement = GetComponent<UnitMovement>();
        _skillCaster = GetComponent<SkillCaster>();
    }

    private void Update()
    {
        if (_runtimeStoppedByDeath || _activeEffects.Count == 0)
            return;

        UpdateActiveEffects(Time.time);
    }

    public bool TryApply(StatusEffectApplication application, bool showPopupEvenIfBlocked = false)
    {
        if (application.TargetUnit == null || application.Definition == null)
            return false;

        if (TryEmitBlockedApplicationFeedback(application, showPopupEvenIfBlocked))
            return false;

        if (IsRuntimeApplicationBlocked(application))
            return false;

        if (IsApplicationBlocked(application))
            return false;

        StatusEffectStackResolution resolution =
            StatusEffectStackingResolver.Resolve(application, _activeEffects, out ActiveStatusEffect existingEffect);

        float now = Time.time;
        switch (resolution)
        {
            case StatusEffectStackResolution.RefreshExisting:
                existingEffect?.ReplaceApplication(application, now);
                if (existingEffect != null)
                    EffectRefreshed?.Invoke(this, existingEffect);
                return existingEffect != null;

            case StatusEffectStackResolution.AddStackToExisting:
                if (existingEffect == null)
                    return AddNewEffect(application, now);

                existingEffect.ReplaceApplication(application, now);
                existingEffect.AddStack(now);
                EffectStackChanged?.Invoke(this, existingEffect);
                return true;

            case StatusEffectStackResolution.ReplaceExisting:
                if (existingEffect != null)
                    RemoveEffect(existingEffect, StatusEffectRemovalReason.Replaced);
                return AddNewEffect(application, now);

            case StatusEffectStackResolution.Ignore:
                return false;

            default:
                return AddNewEffect(application, now);
        }
    }

    public bool HasEffect(SkillEffectKind effectType)
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (activeEffect.Definition.EffectType == effectType)
                return true;
        }

        return false;
    }

    public float GetEffectStrength(SkillEffectKind effectType)
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (activeEffect.Definition.EffectType == effectType)
                return activeEffect.Definition.Strength;
        }

        return 0f;
    }

    public float GetModifierTotal(CombatStatType statType, StatusModifierOperation operation)
    {
        float modifierTotal = 0f;
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null)
                continue;

            if (activeEffect.TryGetStatModifier(statType, operation, out float modifierValue))
            {
                modifierTotal += modifierValue;
            }
        }

        return modifierTotal;
    }

    public void ClearAllEffects(StatusEffectRemovalReason reason = StatusEffectRemovalReason.Explicit)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null)
                continue;

            RemoveEffect(activeEffect, reason);
        }
    }

    public void ClearCombatEffects()
    {
        ClearAllEffects(StatusEffectRemovalReason.EncounterResolved);
    }

    public void HandleIncomingAttack()
    {
    }

    public void HandleOwnerDeath()
    {
        if (_runtimeStoppedByDeath)
            return;

        _runtimeStoppedByDeath = true;
        ClearAllEffects(StatusEffectRemovalReason.OwnerDeath);
        LogDebug($"[{nameof(StatusEffectController)}] '{name}' stopped runtime on owner death.");
    }

    public void RestoreLivingRuntimeState()
    {
        _runtimeStoppedByDeath = false;
    }

    public bool TryGetForcedTarget(out Unit forcedTarget)
    {
        forcedTarget = null;

        // This only exposes the taunt source candidate. The targeting caller decides
        // whether the candidate is valid for the current action/relationship.
        return TryResolveForcedTarget(out forcedTarget);
    }

    private void UpdateActiveEffects(float now)
    {
        ProcessPeriodicTicks(now);
        RemoveExpiredTimedEffects(now);
    }

    private bool TryEmitBlockedApplicationFeedback(StatusEffectApplication application, bool showPopupEvenIfBlocked)
    {
        if (!showPopupEvenIfBlocked || !IsRuntimeApplicationBlocked(application))
            return false;

        ActiveStatusEffect dummyEffect = new(application, Time.time);
        EffectApplied?.Invoke(this, dummyEffect);
        return true;
    }

    private bool IsRuntimeApplicationBlocked(StatusEffectApplication application)
    {
        return _runtimeStoppedByDeath ||
               !isActiveAndEnabled ||
               !ReferenceEquals(application.TargetUnit, _unit) ||
               !IsCombatAlive(_unit);
    }

    private bool AddNewEffect(StatusEffectApplication application, float now)
    {
        ActiveStatusEffect newEffect = new(application, now);
        _activeEffects.Add(newEffect);
        ApplyImmediateStatusRuntimeEffects(newEffect);
        EffectApplied?.Invoke(this, newEffect);
        return true;
    }

    private bool IsApplicationBlocked(StatusEffectApplication application)
    {
        if (application.Definition == null)
            return true;

        SkillEffectKind effectType = application.Definition.EffectType;

        if (IsImmuneToControl && IsControlEffect(effectType))
            return true;

        return false;
    }

    private bool IsControlEffect(SkillEffectKind effectType)
    {
        return effectType == SkillEffectKind.Stun ||
               effectType == SkillEffectKind.Slow;
    }

    private void ProcessPeriodicTicks(float now)
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || !activeEffect.ShouldTick(now))
                continue;

            int ticksToProcess = activeEffect.ConsumePendingTicks(now);
            for (int tickIndex = 0; tickIndex < ticksToProcess; tickIndex++)
                ResolvePeriodicTick(activeEffect);
        }
    }

    private void ResolvePeriodicTick(ActiveStatusEffect activeEffect)
    {
        if (activeEffect == null || activeEffect.Definition == null || _lifeController == null || !IsCombatAlive(_unit))
            return;

        int tickValue = Mathf.Max(0, activeEffect.Definition.TickValue * activeEffect.StackCount);
        if (tickValue <= 0)
            return;

        if (!TryApplyLifeTick(activeEffect, tickValue))
            return;

        EffectTickResolved?.Invoke(this, activeEffect, tickValue);
    }

    private bool TryApplyLifeTick(ActiveStatusEffect activeEffect, int tickValue)
    {
        switch (activeEffect.Definition.EffectType)
        {
            case SkillEffectKind.Heal:
                _lifeController.Heal(tickValue, activeEffect.SourceUnit);
                return true;

            case SkillEffectKind.PoisonBurn:
                _lifeController.TakeDamage(tickValue, activeEffect.SourceUnit, true);
                return true;

            default:
                return false;
        }
    }

    private void RemoveExpiredTimedEffects(float now)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || !activeEffect.IsExpired(now))
                continue;

            RemoveEffect(activeEffect, StatusEffectRemovalReason.Expired);
        }
    }

    private void RemoveEffect(ActiveStatusEffect activeEffect, StatusEffectRemovalReason reason)
    {
        if (activeEffect == null)
            return;

        if (_activeEffects.Remove(activeEffect))
            EffectRemoved?.Invoke(this, activeEffect, reason);
    }

    public void RemoveEffectOfType(SkillEffectKind effectType)
    {
        RemoveEffectsOfType(effectType, StatusEffectRemovalReason.Explicit);
    }

    private void RemoveEffectsOfType(SkillEffectKind effectType, StatusEffectRemovalReason reason)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null || activeEffect.Definition.EffectType != effectType)
                continue;

            RemoveEffect(activeEffect, reason);
        }
    }



    private bool HasMovementRestriction()
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (activeEffect.Definition.RestrictsMovement)
                return true;
        }

        return false;
    }

    private bool HasBlockingActionEffect()
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (activeEffect.Definition.BlocksActions)
                return true;
        }

        return false;
    }

    private bool TryResolveForcedTarget(out Unit forcedTarget)
    {
        forcedTarget = null;

        ActiveStatusEffect latestTaunt = null;
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (!activeEffect.Definition.IsTaunt)
                continue;

            if (latestTaunt == null || activeEffect.AppliedAt > latestTaunt.AppliedAt)
                latestTaunt = activeEffect;
        }

        if (latestTaunt == null)
            return false;

        Unit tauntSource = latestTaunt.SourceUnit;
        if (!IsValidForcedTarget(tauntSource))
            return false;

        forcedTarget = tauntSource;
        return true;
    }

    private void ApplyImmediateStatusRuntimeEffects(ActiveStatusEffect activeEffect)
    {
        if (activeEffect == null || activeEffect.Definition == null)
            return;

        if (activeEffect.Definition.RestrictsMovement)
            _unitMovement?.InterruptMovement();

        if (activeEffect.Definition.BlocksActions)
            _skillCaster?.InterruptCast();

        TryApplyImmediateLifeEffect(activeEffect);
    }

    private void TryApplyImmediateLifeEffect(ActiveStatusEffect activeEffect)
    {
        if (activeEffect == null || activeEffect.Definition == null || _lifeController == null)
            return;

        if (activeEffect.Definition.EffectType != SkillEffectKind.Heal)
            return;

        int healAmount = Mathf.Max(0, activeEffect.Definition.TickValue);
        if (healAmount > 0)
            _lifeController.Heal(healAmount, activeEffect.SourceUnit);
    }

    private bool IsSourceOfEffectInCurrentRoom(SkillEffectKind effectType)
    {
        if (_unit == null || _unit.RoomContext == null)
            return false;

        IReadOnlyList<Unit> roomUnits = _unit.RoomContext.Units;
        if (roomUnits == null)
            return false;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (ReferenceEquals(candidate, _unit) || !IsCombatAlive(candidate) || candidate.StatusEffects == null)
                continue;

            if (candidate.StatusEffects.HasEffectFromSource(effectType, _unit))
                return true;
        }

        return false;
    }

    private bool HasEffectFromSource(SkillEffectKind effectType, Unit sourceUnit)
    {
        if (sourceUnit == null)
            return false;

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (activeEffect.Definition.EffectType != effectType || !ReferenceEquals(activeEffect.SourceUnit, sourceUnit))
                continue;

            return true;
        }

        return false;
    }

    private bool IsValidForcedTarget(Unit sourceUnit)
    {
        return IsCombatAlive(sourceUnit) &&
               sourceUnit.gameObject.activeInHierarchy &&
               _unit != null &&
               _unit.IsHostileTo(sourceUnit) &&
               ReferenceEquals(_unit.RoomContext, sourceUnit.RoomContext);
    }

    private static bool IsCombatAlive(Unit unit)
    {
        return unit != null &&
               unit.IsAlive &&
               unit.LifecycleState == UnitLifecycleState.Alive;
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

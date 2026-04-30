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
    private readonly List<ActiveStatusEffect> _activeEffects = new();
    private bool _runtimeStoppedByDeath;

    public event Action<StatusEffectController, ActiveStatusEffect> EffectApplied;
    public event Action<StatusEffectController, ActiveStatusEffect> EffectRefreshed;
    public event Action<StatusEffectController, ActiveStatusEffect> EffectStackChanged;
    public event Action<StatusEffectController, ActiveStatusEffect, int> EffectTickResolved;
    public event Action<StatusEffectController, ActiveStatusEffect, StatusEffectRemovalReason> EffectRemoved;

public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;
    public bool HasStun => HasEffect(StatusEffectType.Stun);
    public bool HasSilence => HasEffect(StatusEffectType.Silence);
    public bool HasFear => HasEffect(StatusEffectType.Fear);
    public bool HasSleep => HasEffect(StatusEffectType.Sleep);
    public bool HasTaunt => HasEffect(StatusEffectType.Taunt);
    public bool HasInvisibility => HasEffect(StatusEffectType.Invisibility);
    public bool HasInvincibility => HasEffect(StatusEffectType.Invincibility);
    public bool HasIncorruptible => HasEffect(StatusEffectType.Incorruptible);
    public bool HasBerserk => HasEffect(StatusEffectType.Berserk);
    public bool HasLifeSteal => HasEffect(StatusEffectType.LifeSteal);
    public bool HasKnockback => HasEffect(StatusEffectType.Knockback);
    public bool CanAct => !HasBlockingActionEffect();
    public bool CanMove => !HasMovementRestriction();
    public bool CanAttack => !HasStun && !HasFear && !HasSleep;
    public bool CanUseSkills => !HasStun && !HasFear && !HasSleep && !HasSilence && !HasBerserk;
    public bool CanMoveTowardTarget => !HasStun && !HasFear && !HasSleep;
    public bool ShouldFlee => HasFear;
    public bool RestrictsMovement => HasMovementRestriction();
    public bool PreventsSkillCooldownCharge => IsSourceOfEffectInCurrentRoom(StatusEffectType.Taunt);
    public bool IsImmuneToControl => HasIncorruptible;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _lifeController = GetComponent<LifeController>();
        _unitMovement = GetComponent<UnitMovement>();
    }

    private void OnEnable()
    {
        LifeController.OnUnitDied += HandleAnyUnitDied;
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleAnyUnitDied;
    }

    private void Update()
    {
        if (_runtimeStoppedByDeath || _activeEffects.Count == 0)
            return;

        float now = Time.time;
        ProcessTicks(now);
        RemoveExpiredEffects(now);
    }

public bool TryApply(StatusEffectApplication application, bool showPopupEvenIfBlocked = false)
    {
        if (application.TargetUnit == null || application.Definition == null)
            return false;

        bool isBlocked = _runtimeStoppedByDeath || !isActiveAndEnabled || !ReferenceEquals(application.TargetUnit, _unit) || !_unit.IsAlive;

        if (showPopupEvenIfBlocked && isBlocked)
        {
            ActiveStatusEffect dummyEffect = new(application, Time.time);
            EffectApplied?.Invoke(this, dummyEffect);
            return false;
        }

        if (isBlocked)
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

    public bool HasEffect(StatusEffectType effectType)
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

    public float GetEffectStrength(StatusEffectType effectType)
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
        RemoveSleepEffectsWokenByAttack(Time.time, StatusEffectRemovalReason.Explicit);
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

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null || activeEffect.Definition.EffectType != StatusEffectType.Taunt)
                continue;

            Unit sourceUnit = activeEffect.SourceUnit;
            if (!IsValidForcedTarget(sourceUnit))
                continue;

            forcedTarget = sourceUnit;
            return true;
        }

        return false;
    }

private bool AddNewEffect(StatusEffectApplication application, float now)
    {
        ActiveStatusEffect newEffect = new(application, now);
        _activeEffects.Add(newEffect);
        ApplyImmediateRuntimeRestrictions(newEffect);
        
        if (application.Definition.EffectType == StatusEffectType.Heal && _lifeController != null)
        {
            int healAmount = Mathf.Max(0, application.Definition.TickValue);
            if (healAmount > 0)
                _lifeController.Heal(healAmount, application.SourceUnit);
        }
        
        EffectApplied?.Invoke(this, newEffect);
        return true;
    }

private bool IsApplicationBlocked(StatusEffectApplication application)
    {
        if (application.Definition == null)
            return true;

        StatusEffectType effectType = application.Definition.EffectType;

        if (effectType == StatusEffectType.Sleep && HasSleep)
            return true;

        if (IsImmuneToControl && IsControlEffect(effectType))
            return true;

        return false;
    }

    private bool IsControlEffect(StatusEffectType effectType)
    {
        return effectType == StatusEffectType.Stun ||
               effectType == StatusEffectType.Sleep ||
               effectType == StatusEffectType.Fear ||
               effectType == StatusEffectType.Silence ||
               effectType == StatusEffectType.Taunt;
    }

    private void ProcessTicks(float now)
    {
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || !activeEffect.ShouldTick(now))
                continue;

            int ticksToProcess = activeEffect.ConsumePendingTicks(now);
            for (int tickIndex = 0; tickIndex < ticksToProcess; tickIndex++)
                ResolveTick(activeEffect);
        }
    }

    private void ResolveTick(ActiveStatusEffect activeEffect)
    {
        if (activeEffect == null || activeEffect.Definition == null || _lifeController == null || !_lifeController.IsAlive)
            return;

        int tickValue = Mathf.Max(0, activeEffect.Definition.TickValue * activeEffect.StackCount);
        if (tickValue <= 0)
            return;

        switch (activeEffect.Definition.EffectType)
        {
            case StatusEffectType.HealOverTime:
                _lifeController.Heal(tickValue, activeEffect.SourceUnit);
                break;

            case StatusEffectType.DamageOverTime:
                _lifeController.TakeDamage(tickValue, activeEffect.SourceUnit);
                break;

            default:
                return;
        }

        EffectTickResolved?.Invoke(this, activeEffect, tickValue);
    }

    private void RemoveExpiredEffects(float now)
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

    public void RemoveEffectOfType(StatusEffectType effectType)
    {
        RemoveEffectsOfType(effectType, StatusEffectRemovalReason.Explicit);
    }

    private void RemoveEffectsOfType(StatusEffectType effectType, StatusEffectRemovalReason reason)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || activeEffect.Definition == null || activeEffect.Definition.EffectType != effectType)
                continue;

            RemoveEffect(activeEffect, reason);
        }
    }

    private void RemoveSleepEffectsWokenByAttack(float now, StatusEffectRemovalReason reason)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null || !activeEffect.CanBeWokenByAttack(now))
                continue;

            RemoveEffect(activeEffect, reason);
        }
    }

    private void HandleAnyUnitDied(Unit unit)
    {
        if (!ReferenceEquals(unit, _unit))
            return;

        HandleOwnerDeath();
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

    private void ApplyImmediateRuntimeRestrictions(ActiveStatusEffect activeEffect)
    {
        if (activeEffect == null || activeEffect.Definition == null)
            return;

        if (activeEffect.Definition.RestrictsMovement)
            _unitMovement?.InterruptMovement();
    }

    private bool IsSourceOfEffectInCurrentRoom(StatusEffectType effectType)
    {
        if (_unit == null || _unit.RoomContext == null)
            return false;

        IReadOnlyList<Unit> roomUnits = _unit.RoomContext.Units;
        if (roomUnits == null)
            return false;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (candidate == null || ReferenceEquals(candidate, _unit) || !candidate.IsAlive || candidate.StatusEffects == null)
                continue;

            if (candidate.StatusEffects.HasEffectFromSource(effectType, _unit))
                return true;
        }

        return false;
    }

    private bool HasEffectFromSource(StatusEffectType effectType, Unit sourceUnit)
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
        return sourceUnit != null &&
               sourceUnit.IsAlive &&
               sourceUnit.gameObject.activeInHierarchy &&
               _unit != null &&
               _unit.IsHostileTo(sourceUnit) &&
               ReferenceEquals(_unit.RoomContext, sourceUnit.RoomContext);
    }

private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

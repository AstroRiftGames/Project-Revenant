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
    private readonly List<ActiveStatusEffect> _activeEffects = new();
    private bool _runtimeStoppedByDeath;

    public event Action<StatusEffectController, ActiveStatusEffect> EffectApplied;
    public event Action<StatusEffectController, ActiveStatusEffect> EffectRefreshed;
    public event Action<StatusEffectController, ActiveStatusEffect> EffectStackChanged;
    public event Action<StatusEffectController, ActiveStatusEffect, int> EffectTickResolved;
    public event Action<StatusEffectController, ActiveStatusEffect, StatusEffectRemovalReason> EffectRemoved;

    public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;
    public bool HasStun => HasEffect(StatusEffectType.Stun);
    public bool CanAct => !HasStun;
    public bool RestrictsMovement => HasMovementRestriction();

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _lifeController = GetComponent<LifeController>();
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

    public bool TryApply(StatusEffectApplication application)
    {
        if (application.TargetUnit == null || application.Definition == null)
            return false;

        if (_runtimeStoppedByDeath || !isActiveAndEnabled || !ReferenceEquals(application.TargetUnit, _unit) || !_unit.IsAlive)
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

    public float GetModifierTotal(CombatStatType statType, StatusModifierOperation operation)
    {
        float modifierTotal = 0f;
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _activeEffects[i];
            if (activeEffect == null)
                continue;

            if (activeEffect.TryGetStatModifier(statType, operation, out float modifierValue))
                modifierTotal += modifierValue;
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

    private bool AddNewEffect(StatusEffectApplication application, float now)
    {
        ActiveStatusEffect newEffect = new(application, now);
        _activeEffects.Add(newEffect);
        EffectApplied?.Invoke(this, newEffect);
        return true;
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

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

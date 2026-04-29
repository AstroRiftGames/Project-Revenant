using System;
using UnityEngine;

[Serializable]
public struct AppliedStatusEffectSpec
{
    [SerializeField] private StatusEffectDefinition _definition;

    public StatusEffectDefinition Definition => _definition;
}

public readonly struct StatusEffectApplication
{
    public StatusEffectApplication(Unit targetUnit, Unit sourceUnit, SkillData sourceSkill, StatusEffectDefinition definition)
    {
        TargetUnit = targetUnit;
        SourceUnit = sourceUnit;
        SourceSkill = sourceSkill;
        Definition = definition;
    }

    public Unit TargetUnit { get; }
    public Unit SourceUnit { get; }
    public SkillData SourceSkill { get; }
    public StatusEffectDefinition Definition { get; }
}

public sealed class ActiveStatusEffect
{
    private const float SleepWakeProtectionSeconds = 0.1f;

    public ActiveStatusEffect(StatusEffectApplication application, float now)
    {
        Application = application;
        Definition = application.Definition;
        AppliedAt = now;
        StackCount = 1;
        Refresh(now);
    }

    public StatusEffectApplication Application { get; private set; }
    public StatusEffectDefinition Definition { get; }
    public Unit SourceUnit => Application.SourceUnit;
    public SkillData SourceSkill => Application.SourceSkill;
    public Unit TargetUnit => Application.TargetUnit;
    public float AppliedAt { get; }
    public int StackCount { get; private set; }
    public float ExpiresAt { get; private set; }
    public float NextTickAt { get; private set; }

    public bool IsExpired(float now)
    {
        return Definition.HasTimedDuration && now >= ExpiresAt;
    }

    public bool ShouldTick(float now)
    {
        return Definition.HasPeriodicTicks && now >= NextTickAt;
    }

    public void Refresh(float now)
    {
        if (Definition.HasTimedDuration)
            ExpiresAt = now + Definition.DurationSeconds;

        if (Definition.HasPeriodicTicks)
            NextTickAt = now + Definition.TickIntervalSeconds;
    }

    public void ReplaceApplication(StatusEffectApplication application, float now)
    {
        Application = application;
        Refresh(now);
    }

    public void AddStack(float now)
    {
        StackCount = Mathf.Clamp(StackCount + 1, 1, Definition.MaxStacks);
        Refresh(now);
    }

    public int ConsumePendingTicks(float now)
    {
        if (!Definition.HasPeriodicTicks)
            return 0;

        int ticksToProcess = 0;
        while (now >= NextTickAt)
        {
            ticksToProcess++;
            NextTickAt += Definition.TickIntervalSeconds;
        }

        return ticksToProcess;
    }

    public bool TryGetStatModifier(CombatStatType statType, StatusModifierOperation operation, out float modifierValue)
    {
        modifierValue = 0f;
        if (!Definition.AffectsStats)
            return false;

        StatusEffectStatModifier modifier = Definition.StatModifier;
        if (modifier.StatType != statType || modifier.Operation != operation)
            return false;

        modifierValue = modifier.Value * StackCount;
        return true;
    }

    public bool CanBeWokenByAttack(float now)
    {
        if (Definition == null || Definition.EffectType != StatusEffectType.Sleep)
            return false;

        return now >= AppliedAt + SleepWakeProtectionSeconds;
    }
}

using System;

public enum LabEffectKind
{
    Damage,
    Heal,
    Shield,
    Stun,
    Slow,
    PoisonBurn,
    Haste,
    StrengthBuff,
    HealOverTime,
    Summon,
    Knockback
}

public enum LabModifierKind
{
    None,
    Splash,
    Penetrating,
    Bounce,
    Explosive,
    Persistent,
    Periodic
}

public static class SkillCompositionMapper
{
    public static SkillEffectKind ToSkillEffectKind(LabEffectKind labKind)
    {
        return labKind switch
        {
            LabEffectKind.Damage => SkillEffectKind.Damage,
            LabEffectKind.Heal => SkillEffectKind.Heal,
            LabEffectKind.Shield => SkillEffectKind.Shield,
            LabEffectKind.Stun => SkillEffectKind.Stun,
            LabEffectKind.Slow => SkillEffectKind.Slow,
            LabEffectKind.PoisonBurn => SkillEffectKind.PoisonBurn,
            LabEffectKind.Haste => SkillEffectKind.Haste,
            LabEffectKind.StrengthBuff => SkillEffectKind.StrengthBuff,
            LabEffectKind.HealOverTime => SkillEffectKind.Heal,
            LabEffectKind.Summon => SkillEffectKind.Summon,
            LabEffectKind.Knockback => SkillEffectKind.Knockback,
            _ => throw new ArgumentOutOfRangeException(nameof(labKind), labKind, $"Unsupported LabEffectKind: {labKind}")
        };
    }

    public static LabEffectKind? ToLabEffectKind(SkillEffectKind skillKind)
    {
        return skillKind switch
        {
            SkillEffectKind.Damage => LabEffectKind.Damage,
            SkillEffectKind.Heal => LabEffectKind.Heal,
            SkillEffectKind.Shield => LabEffectKind.Shield,
            SkillEffectKind.Stun => LabEffectKind.Stun,
            SkillEffectKind.Slow => LabEffectKind.Slow,
            SkillEffectKind.PoisonBurn => LabEffectKind.PoisonBurn,
            SkillEffectKind.Haste => LabEffectKind.Haste,
            SkillEffectKind.StrengthBuff => LabEffectKind.StrengthBuff,
            SkillEffectKind.Summon => LabEffectKind.Summon,
            SkillEffectKind.Knockback => LabEffectKind.Knockback,
            _ => null
        };
    }

    public static SkillModifierKind ToSkillModifierKind(LabModifierKind labKind)
    {
        return labKind switch
        {
            LabModifierKind.Splash => SkillModifierKind.Splash,
            LabModifierKind.Penetrating => SkillModifierKind.Penetrating,
            LabModifierKind.Bounce => SkillModifierKind.Bounce,
            LabModifierKind.Explosive => SkillModifierKind.Explosive,
            LabModifierKind.Persistent => SkillModifierKind.Persistent,
            LabModifierKind.Periodic => SkillModifierKind.Periodic,
            _ => throw new ArgumentOutOfRangeException(nameof(labKind), labKind, $"Unsupported LabModifierKind: {labKind}")
        };
    }

    public static LabModifierKind ToLabModifierKind(SkillModifierKind skillKind)
    {
        return skillKind switch
        {
            SkillModifierKind.Splash => LabModifierKind.Splash,
            SkillModifierKind.Penetrating => LabModifierKind.Penetrating,
            SkillModifierKind.Bounce => LabModifierKind.Bounce,
            SkillModifierKind.Explosive => LabModifierKind.Explosive,
            SkillModifierKind.Persistent => LabModifierKind.Persistent,
            SkillModifierKind.Periodic => LabModifierKind.Periodic,
            SkillModifierKind.Cumulative => LabModifierKind.None,
            SkillModifierKind.Expandable => LabModifierKind.None,
            _ => LabModifierKind.None
        };
    }

    public static bool IsStatusEffect(LabEffectKind kind)
    {
        return kind == LabEffectKind.Stun ||
               kind == LabEffectKind.Slow ||
               kind == LabEffectKind.PoisonBurn ||
               kind == LabEffectKind.Haste ||
               kind == LabEffectKind.StrengthBuff ||
               kind == LabEffectKind.HealOverTime;
    }

    public static bool IsBeneficialEffect(LabEffectKind kind)
    {
        return kind == LabEffectKind.Heal ||
               kind == LabEffectKind.HealOverTime ||
               kind == LabEffectKind.Shield ||
               kind == LabEffectKind.Haste ||
               kind == LabEffectKind.StrengthBuff;
    }

    public static bool IsOffensiveEffect(LabEffectKind kind)
    {
        return kind == LabEffectKind.Damage ||
               kind == LabEffectKind.Stun ||
               kind == LabEffectKind.Slow ||
               kind == LabEffectKind.PoisonBurn ||
               kind == LabEffectKind.Knockback;
    }

    public static bool HasNumericValue(LabEffectKind kind)
    {
        return kind == LabEffectKind.Damage ||
               kind == LabEffectKind.Heal ||
               kind == LabEffectKind.Shield ||
               kind == LabEffectKind.HealOverTime;
    }
}
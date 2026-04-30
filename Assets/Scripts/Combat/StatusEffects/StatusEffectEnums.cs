public enum StatusEffectType
{
    Stun,
    Silence,
    Fear,
    Sleep,
    Taunt,
    Heal,
    HealOverTime,
    DamageOverTime,
    StatModifierBuff,
    StatModifierDebuff,
    Invisibility,
    Invincibility,
    Incorruptible,
    Berserk,
    LifeSteal,
    Knockback
}

public enum StatusEffectDurationMode
{
    Timed,
    PermanentUntilDeath
}

public enum CombatStatType
{
    MoveSpeed,
    Damage,
    Range,
    Accuracy,
    Defense
}

public enum EffectStackingMode
{
    RefreshDuration,
    AddStack,
    IndependentInstance,
    ReplaceByStronger,
    IgnoreIfSameSource
}

public enum StatusModifierOperation
{
    Additive,
    Multiplier
}

public enum StatusEffectRemovalReason
{
    Expired,
    OwnerDeath,
    Explicit,
    EncounterResolved,
    Replaced
}

public enum StatusVisualStyle
{
    None,
    Stun,
    HealOverTime,
    DamageOverTime,
    DamageOverTimePermanent,
    Buff,
    Debuff,
    Invincible,
    Invisible,
    Incorruptible,
    LifeSteal,
    Knockback
}

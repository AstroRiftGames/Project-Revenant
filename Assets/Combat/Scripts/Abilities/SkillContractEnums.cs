using UnityEngine;

// Forward declaration for StatusEffectDefinition used in SkillCompositionEffect
// The full type lives in StatusEffects/StatusEffectDefinition.cs

public enum PrimaryTargetRequirement
{
    None,
    Hostile,
    Ally,
    Self,
    GroundCell
}

public enum ImpactTargetRequirement
{
    Hostile,
    Ally,
    Self,
    Any
}

public enum TargetSelectionMode
{
    None,
    Closest,
    LowestHealth,
    HighestBasicDamage,
    AllyLowestHealth,
    AllyRolePriority,
    RoleBasedOffensive
}

public enum TargetFallbackMode
{
    Cancel,
    Retarget,
    ContinueFromCurrentContext
}

public enum SkillTrajectory
{
    Hitscan,
    Projectile
}

public enum ImpactPattern
{
    Direct,
    Area,
    Line,
    MultiTarget
}

public enum SkillExecutionMode
{
    Instant,
    CastTime,
    Channel
}

public enum SkillEffectKind
{
    Damage,            // Daño
    Heal,              // Curación
    Shield,            // Escudo
    Haste,             // Aceleración
    StrengthBuff,      // Aumento de fuerza
    Slow,              // Slow
    Stun,              // Stun
    PoisonBurn,        // Veneno/Quemadura
    Summon,            // Invocar
    Knockback,         // Empuje
    Buff,              // Buff para aumentar stats
    Debuff,            // Debuff para reducir stats
    StatModifierDebuff, // Debuff de estadística genérico
    Taunt,              // Provocar, forzar target hacia el provocador
    Blind,               // Cegar, reduce accuracy y afecta ataques básicos
    Burn                // Quemadura (VFX Debug)
}

public enum SkillModifierKind
{
    Persistent,        // Persistente
    Penetrating,       // Penetrante
    Bounce,            // Rebote
    Explosive,         // Explosivo
    Splash,            // Salpicadura
    Cumulative,        // Acumulativo
    Periodic,          // Periódico
    Expandable         // Expandible
}

public enum SummonAnchorMode
{
    AroundCaster,
    AroundPrimaryTarget,
    AroundImpactCenter,
    AtTargetCell
}

[System.Serializable]
public struct SkillCompositionEffect
{
    [SerializeField] private SkillEffectKind _effectKind;
    [SerializeField] private int _value;
    [SerializeField] private float _duration;
    [SerializeField] private float _interval;
    [SerializeField] private int _maxStacks;
    [SerializeField] private UnitData _summonedUnit;
    [SerializeField] private StatusEffectDefinition _statusDefinition;
    [SerializeField] private SummonAnchorMode _summonAnchorMode;

    public SkillEffectKind EffectKind => _effectKind;
    public int Value => _value;
    public float Duration => _duration;
    public float Interval => _interval;
    public int MaxStacks => _maxStacks;
    public UnitData SummonedUnit => _summonedUnit;
    public StatusEffectDefinition StatusDefinition => _statusDefinition;
    public SummonAnchorMode SummonAnchorMode => _summonAnchorMode;

    public SkillCompositionEffect(SkillEffectKind kind, int value, float duration, float interval, int maxStacks, UnitData summonedUnit = null)
    {
        _effectKind = kind;
        _value = value;
        _duration = duration;
        _interval = interval;
        _maxStacks = maxStacks;
        _summonedUnit = summonedUnit;
        _statusDefinition = null;
        _summonAnchorMode = SummonAnchorMode.AroundImpactCenter;
    }

    public void SetStatusDefinition(StatusEffectDefinition definition)
    {
        _statusDefinition = definition;
    }

    public void SetSummonAnchorMode(SummonAnchorMode mode)
    {
        _summonAnchorMode = mode;
    }
}

[System.Serializable]
public struct SkillCompositionModifierData
{
    [SerializeField] private SkillModifierKind _modifierKind;
    [SerializeField] private int _bounceMaxBounces;
    [SerializeField] private int _bounceRangeInCells;
    [SerializeField] private bool _bounceCanBounceToPrimaryTargetAgain;
    [SerializeField] private int _explosiveRadiusInCells;
    [SerializeField] private bool _explosiveIncludePrimaryImpactTarget;

    public SkillModifierKind ModifierKind => _modifierKind;
    public int BounceMaxBounces => _bounceMaxBounces;
    public int BounceRangeInCells => _bounceRangeInCells;
    public bool BounceCanBounceToPrimaryTargetAgain => _bounceCanBounceToPrimaryTargetAgain;
    public int ExplosiveRadiusInCells => _explosiveRadiusInCells;
    public bool ExplosiveIncludePrimaryImpactTarget => _explosiveIncludePrimaryImpactTarget;

    public SkillCompositionModifierData(SkillModifierKind kind)
    {
        _modifierKind = kind;
        _bounceMaxBounces = 0;
        _bounceRangeInCells = 0;
        _bounceCanBounceToPrimaryTargetAgain = false;
        _explosiveRadiusInCells = 0;
        _explosiveIncludePrimaryImpactTarget = true;
    }

    public void SetBounceParams(int maxBounces, int rangeInCells, bool canBounceToPrimaryAgain)
    {
        _bounceMaxBounces = maxBounces;
        _bounceRangeInCells = rangeInCells;
        _bounceCanBounceToPrimaryTargetAgain = canBounceToPrimaryAgain;
    }

    public void SetExplosiveParams(int radiusInCells, bool includePrimary)
    {
        _explosiveRadiusInCells = radiusInCells;
        _explosiveIncludePrimaryImpactTarget = includePrimary;
    }
}

public enum SkillCompositionState
{
    Official,
    DebugOnly,
    Provisional,
    NonOfficial,
    RequiresDesignDecision
}

public class CombatSummonedUnitRuntimeMarker : MonoBehaviour
{
}

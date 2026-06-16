using UnityEngine;

public class CreatureVariantLabState
{
    public UnitFaction faction = UnitFaction.Human;
    public UnitRole role = UnitRole.DPS;
    public UnitTeam team = UnitTeam.Ally;

    public string unitName = "NewUnit";
    public UnitCombatStyle combatStyle = UnitCombatStyle.Default;
    public UnitTargetingMode targetingMode = UnitTargetingMode.RolePriority;

    public int maxHealth = 10;
    public int attackDamage = 1;
    public float attackCooldown = 0.75f;
    public int attackRangeInCells = 1;
    public int preferredDistanceInCells = 1;
    public float accuracy = 0.85f;
    public float evasion = 0.1f;
    public int defense = 0;
    public float moveSpeed = 2.5f;
    public float visionRange = 5f;

    public int manaCostToRecruit = 1;
    public int manaCostToAbsorbSoul = 1;

    public ImpactPattern delivery = ImpactPattern.Direct;
    public PrimaryTargetRequirement primaryTarget = PrimaryTargetRequirement.Hostile;
    public LabEffectKind effect = LabEffectKind.Damage;
    public LabModifierKind modifier = LabModifierKind.None;

    public int value = 10;
    public float duration = 3f;
    public float interval = 1f;
    public int radiusInCells = 1;
    public int rangeInCells = 3;
    public int lineLengthInCells = 5;
    public int knockbackCells = 2;

    public UnitData summonedUnit;
    public SummonAnchorMode summonAnchorMode = SummonAnchorMode.AroundImpactCenter;

    public int bounceMaxBounces = 3;
    public int bounceRangeInCells = 2;
    public bool bounceCanBounceToPrimaryTargetAgain;

    public int explosiveRadiusInCells = 2;
    public bool explosiveIncludePrimaryImpactTarget = true;

    public StatusEffectDefinition statusDefinition;

    public GameObject prefabOverride;
    public UnitData importSource;

    public string saveSkillName = "";
}
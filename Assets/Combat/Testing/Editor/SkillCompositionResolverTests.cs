using NUnit.Framework;
using UnityEngine;

public static class SkillCompositionResolverTests
{
    [Test]
    public static void ResolveImpactTargetRequirement_Self_ReturnsSelf()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Self, effect = LabEffectKind.Damage };
        Assert.AreEqual(ImpactTargetRequirement.Self, SkillCompositionResolver.ResolveImpactTargetRequirement(state));
    }

    [Test]
    public static void ResolveImpactTargetRequest_DamageHostile_ReturnsHostile()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Hostile, effect = LabEffectKind.Damage };
        Assert.AreEqual(ImpactTargetRequirement.Hostile, SkillCompositionResolver.ResolveImpactTargetRequirement(state));
    }

    [Test]
    public static void ResolveImpactTargetRequirement_HealAlly_ReturnsAlly()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Ally, effect = LabEffectKind.Heal };
        Assert.AreEqual(ImpactTargetRequirement.Ally, SkillCompositionResolver.ResolveImpactTargetRequirement(state));
    }

    [Test]
    public static void ResolveImpactTargetRequirement_ShieldHostile_ReturnsAlly()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Hostile, effect = LabEffectKind.Shield };
        Assert.AreEqual(ImpactTargetRequirement.Ally, SkillCompositionResolver.ResolveImpactTargetRequirement(state));
    }

    [Test]
    public static void ResolveTargetSelectionMode_Self_ReturnsNone()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Self };
        Assert.AreEqual(TargetSelectionMode.None, SkillCompositionResolver.ResolveTargetSelectionMode(state));
    }

    [Test]
    public static void ResolveTargetSelectionMode_Hostile_ReturnsRoleBasedOffensive()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Hostile };
        Assert.AreEqual(TargetSelectionMode.RoleBasedOffensive, SkillCompositionResolver.ResolveTargetSelectionMode(state));
    }

    [Test]
    public static void ResolveImpactCenterMode_Self_ReturnsCaster()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Self };
        Assert.AreEqual(ImpactCenterMode.Caster, SkillCompositionResolver.ResolveImpactCenterMode(state));
    }

    [Test]
    public static void ResolveImpactCenterMode_Hostile_ReturnsPrimaryTarget()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Hostile };
        Assert.AreEqual(ImpactCenterMode.PrimaryTarget, SkillCompositionResolver.ResolveImpactCenterMode(state));
    }

    [Test]
    public static void ResolveRangeInCells_Self_ReturnsZero()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Self, rangeInCells = 5 };
        Assert.AreEqual(0, SkillCompositionResolver.ResolveRangeInCells(state));
    }

    [Test]
    public static void ResolveRangeInCells_Hostile_ReturnsRange()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.Hostile, rangeInCells = 5 };
        Assert.AreEqual(5, SkillCompositionResolver.ResolveRangeInCells(state));
    }

    [Test]
    public static void ResolveMaxTargets_Direct_ReturnsConfigValue()
    {
        var config = ScriptableObject.CreateInstance<CreatureVariantLabConfig>();
        config.maxTargets = new CreatureVariantLabConfig.MaxTargetsConfig { direct = 1, line = 10, area = 10, fallback = 10 };
        var state = new CreatureVariantLabState { delivery = ImpactPattern.Direct };
        Assert.AreEqual(1, SkillCompositionResolver.ResolveMaxTargets(state, config));
        Object.DestroyImmediate(config);
    }

    [Test]
    public static void ResolveMaxTargets_Area_ReturnsConfigValue()
    {
        var config = ScriptableObject.CreateInstance<CreatureVariantLabConfig>();
        config.maxTargets = new CreatureVariantLabConfig.MaxTargetsConfig { direct = 1, line = 10, area = 6, fallback = 10 };
        var state = new CreatureVariantLabState { delivery = ImpactPattern.Area };
        Assert.AreEqual(6, SkillCompositionResolver.ResolveMaxTargets(state, config));
        Object.DestroyImmediate(config);
    }

    [Test]
    public static void ResolveMaxTargets_NullConfig_UsesFallback()
    {
        var state = new CreatureVariantLabState { delivery = ImpactPattern.Direct };
        Assert.AreEqual(1, SkillCompositionResolver.ResolveMaxTargets(state, null));
    }

    [Test]
    public static void ResolveTrajectory_Always_ReturnsHitscan()
    {
        var state = new CreatureVariantLabState();
        Assert.AreEqual(SkillTrajectory.Hitscan, SkillCompositionResolver.ResolveTrajectory(state));
    }

    [Test]
    public static void ResolveExecutionMode_Always_ReturnsInstant()
    {
        var state = new CreatureVariantLabState();
        Assert.AreEqual(SkillExecutionMode.Instant, SkillCompositionResolver.ResolveExecutionMode(state));
    }
}
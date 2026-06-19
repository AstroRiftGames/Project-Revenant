using System.Collections.Generic;
using NUnit.Framework;

public static class SkillCompositionRulesTests
{
    [Test]
    public static void IsRoleDeliveryCompatible_DPS_Direct_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsRoleDeliveryCompatible(UnitRole.DPS, ImpactPattern.Direct));
    }

    [Test]
    public static void IsRoleDeliveryCompatible_DPS_Area_Invalid()
    {
        Assert.IsFalse(SkillCompositionRules.IsRoleDeliveryCompatible(UnitRole.DPS, ImpactPattern.Area));
    }

    [Test]
    public static void IsRoleDeliveryCompatible_Tank_Area_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsRoleDeliveryCompatible(UnitRole.Tank, ImpactPattern.Area));
    }

    [Test]
    public static void IsRoleDeliveryCompatible_Tank_Direct_Invalid()
    {
        Assert.IsFalse(SkillCompositionRules.IsRoleDeliveryCompatible(UnitRole.Tank, ImpactPattern.Direct));
    }

    [Test]
    public static void IsRoleDeliveryCompatible_Support_Direct_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsRoleDeliveryCompatible(UnitRole.Support, ImpactPattern.Direct));
    }

    [Test]
    public static void IsRoleDeliveryCompatible_Support_Area_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsRoleDeliveryCompatible(UnitRole.Support, ImpactPattern.Area));
    }

    [Test]
    public static void IsEffectTargetCompatible_DamageHostile_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsEffectTargetCompatible(LabEffectKind.Damage, PrimaryTargetRequirement.Hostile));
    }

    [Test]
    public static void IsEffectTargetCompatible_DamageAlly_Invalid()
    {
        Assert.IsFalse(SkillCompositionRules.IsEffectTargetCompatible(LabEffectKind.Damage, PrimaryTargetRequirement.Ally));
    }

    [Test]
    public static void IsEffectTargetCompatible_HealHostile_Invalid()
    {
        Assert.IsFalse(SkillCompositionRules.IsEffectTargetCompatible(LabEffectKind.Heal, PrimaryTargetRequirement.Hostile));
    }

    [Test]
    public static void IsEffectTargetCompatible_HealAlly_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsEffectTargetCompatible(LabEffectKind.Heal, PrimaryTargetRequirement.Ally));
    }

    [Test]
    public static void IsEffectTargetCompatible_ShieldSelf_Valid()
    {
        Assert.IsTrue(SkillCompositionRules.IsEffectTargetCompatible(LabEffectKind.Shield, PrimaryTargetRequirement.Self));
    }

    [Test]
    public static void IsProductionCatalogCompatible_DamageDirect_Valid()
    {
        var state = new CreatureVariantLabState { effect = LabEffectKind.Damage, modifier = LabModifierKind.None, primaryTarget = PrimaryTargetRequirement.Hostile };
        Assert.IsTrue(SkillCompositionRules.IsProductionCatalogCompatible(state));
    }

    [Test]
    public static void IsProductionCatalogCompatible_Summon_Invalid()
    {
        var state = new CreatureVariantLabState { effect = LabEffectKind.Summon };
        Assert.IsFalse(SkillCompositionRules.IsProductionCatalogCompatible(state));
    }

    [Test]
    public static void IsProductionCatalogCompatible_Bounce_Invalid()
    {
        var state = new CreatureVariantLabState { modifier = LabModifierKind.Bounce };
        Assert.IsFalse(SkillCompositionRules.IsProductionCatalogCompatible(state));
    }

    [Test]
    public static void IsProductionCatalogCompatible_Taunt_Invalid()
    {
        var state = new CreatureVariantLabState { effect = LabEffectKind.Taunt };
        Assert.IsFalse(SkillCompositionRules.IsProductionCatalogCompatible(state));
    }

    [Test]
    public static void IsProductionCatalogCompatible_Blind_Invalid()
    {
        var state = new CreatureVariantLabState { effect = LabEffectKind.Blind };
        Assert.IsFalse(SkillCompositionRules.IsProductionCatalogCompatible(state));
    }

    [Test]
    public static void IsProductionCatalogCompatible_GroundCell_Invalid()
    {
        var state = new CreatureVariantLabState { primaryTarget = PrimaryTargetRequirement.GroundCell };
        Assert.IsFalse(SkillCompositionRules.IsProductionCatalogCompatible(state));
    }
}

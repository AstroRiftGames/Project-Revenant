using NUnit.Framework;

public static class SkillCompositionMapperTests
{
    [Test]
    public static void ToSkillEffectKind_CoversAllLabKinds()
    {
        var kinds = System.Enum.GetValues(typeof(LabEffectKind));
        foreach (LabEffectKind kind in kinds)
        {
            SkillEffectKind mapped = SkillCompositionMapper.ToSkillEffectKind(kind);
            Assert.NotZero((int)mapped, $"ToSkillEffectKind({kind}) returned default/zero.");
        }
    }

    [Test]
    public static void ToSkillEffectKind_Damage_MapsCorrectly()
    {
        Assert.AreEqual(SkillEffectKind.Damage, SkillCompositionMapper.ToSkillEffectKind(LabEffectKind.Damage));
    }

    [Test]
    public static void ToSkillEffectKind_HealOverTime_MapsToHeal()
    {
        Assert.AreEqual(SkillEffectKind.Heal, SkillCompositionMapper.ToSkillEffectKind(LabEffectKind.HealOverTime));
    }

    [Test]
    public static void ToLabEffectKind_NormalKinds_AreReversible()
    {
        Assert.AreEqual(LabEffectKind.Damage, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Damage));
        Assert.AreEqual(LabEffectKind.Heal, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Heal));
        Assert.AreEqual(LabEffectKind.Shield, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Shield));
        Assert.AreEqual(LabEffectKind.Stun, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Stun));
        Assert.AreEqual(LabEffectKind.Slow, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Slow));
        Assert.AreEqual(LabEffectKind.PoisonBurn, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.PoisonBurn));
        Assert.AreEqual(LabEffectKind.Haste, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Haste));
        Assert.AreEqual(LabEffectKind.StrengthBuff, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.StrengthBuff));
        Assert.AreEqual(LabEffectKind.Summon, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Summon));
        Assert.AreEqual(LabEffectKind.Knockback, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Knockback));
        Assert.AreEqual(LabEffectKind.Taunt, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Taunt));
        Assert.AreEqual(LabEffectKind.Blind, SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Blind));
    }

    [Test]
    public static void ToLabEffectKind_RuntimeOnlyKinds_ReturnNull()
    {
        Assert.IsNull(SkillCompositionMapper.ToLabEffectKind(SkillEffectKind.Buff));
    }

    [Test]
    public static void IsBeneficialEffect_Heal_IsBeneficial()
    {
        Assert.IsTrue(SkillCompositionMapper.IsBeneficialEffect(LabEffectKind.Heal));
    }

    [Test]
    public static void IsBeneficialEffect_Damage_IsNotBeneficial()
    {
        Assert.IsFalse(SkillCompositionMapper.IsBeneficialEffect(LabEffectKind.Damage));
    }

    [Test]
    public static void IsOffensiveEffect_Damage_IsOffensive()
    {
        Assert.IsTrue(SkillCompositionMapper.IsOffensiveEffect(LabEffectKind.Damage));
    }

    [Test]
    public static void IsOffensiveEffect_Heal_IsNotOffensive()
    {
        Assert.IsFalse(SkillCompositionMapper.IsOffensiveEffect(LabEffectKind.Heal));
    }

    [Test]
    public static void IsStatusEffect_Stun_IsStatus()
    {
        Assert.IsTrue(SkillCompositionMapper.IsStatusEffect(LabEffectKind.Stun));
    }

    [Test]
    public static void IsStatusEffect_Damage_IsNotStatus()
    {
        Assert.IsFalse(SkillCompositionMapper.IsStatusEffect(LabEffectKind.Damage));
    }

    [Test]
    public static void HasNumericValue_Damage_HasValue()
    {
        Assert.IsTrue(SkillCompositionMapper.HasNumericValue(LabEffectKind.Damage));
    }

    [Test]
    public static void HasNumericValue_Stun_NoValue()
    {
        Assert.IsFalse(SkillCompositionMapper.HasNumericValue(LabEffectKind.Stun));
    }

    [Test]
    public static void ToSkillModifierKind_CoversAllLabKinds()
    {
        var kinds = System.Enum.GetValues(typeof(LabModifierKind));
        foreach (LabModifierKind kind in kinds)
        {
            if (kind == LabModifierKind.None) continue;
            SkillModifierKind mapped = SkillCompositionMapper.ToSkillModifierKind(kind);
            Assert.NotZero((int)mapped, $"ToSkillModifierKind({kind}) returned default/zero.");
        }
    }

    [Test]
    public static void ProductionSupportCatalog_Persistent_IsMissingRuntime()
    {
        Assert.AreEqual(SkillProductionSupportStatus.MissingRuntime, SkillProductionSupportCatalog.GetModifierStatus(LabModifierKind.Persistent));
    }

    [Test]
    public static void ProductionSupportCatalog_Bounce_IsRuntimePartial()
    {
        Assert.AreEqual(SkillProductionSupportStatus.RuntimePartial, SkillProductionSupportCatalog.GetModifierStatus(LabModifierKind.Bounce));
    }

    [Test]
    public static void ProductionSupportCatalog_Summon_IsExperimental()
    {
        Assert.AreEqual(SkillProductionSupportStatus.Experimental, SkillProductionSupportCatalog.GetEffectStatus(LabEffectKind.Summon));
    }
}

using System.Collections.Generic;
using NUnit.Framework;

public static class SkillCompositionParameterValidatorTests
{
    [Test]
    public static void ValidatePrefab_NullPrefab_AddsError()
    {
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidatePrefab(null, issues);
        Assert.IsTrue(issues.Count > 0);
        Assert.IsTrue(issues[0].severity == ValidationSeverity.Error);
    }

    [Test]
    public static void ValidateUnitStats_ZeroMaxHp_AddsError()
    {
        var state = new CreatureVariantLabState { maxHealth = 0 };
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidateUnitStats(state, issues);
        bool found = false;
        foreach (var issue in issues)
            if (issue.severity == ValidationSeverity.Error && issue.message.Contains("Max HP"))
                found = true;
        Assert.IsTrue(found);
    }

    [Test]
    public static void ValidateUnitStats_EmptyName_AddsError()
    {
        var state = new CreatureVariantLabState { unitName = "" };
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidateUnitStats(state, issues);
        bool found = false;
        foreach (var issue in issues)
            if (issue.severity == ValidationSeverity.Error && issue.message.Contains("Unit name"))
                found = true;
        Assert.IsTrue(found);
    }

    [Test]
    public static void ValidateRoleDelivery_DPSArea_Invalid()
    {
        var state = new CreatureVariantLabState { role = UnitRole.DPS, delivery = ImpactPattern.Area };
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidateRoleDelivery(state, issues);
        Assert.IsTrue(issues.Count > 0);
    }

    [Test]
    public static void ValidateEffectTargetCompatibility_HealHostile_Invalid()
    {
        var state = new CreatureVariantLabState { effect = LabEffectKind.Heal, primaryTarget = PrimaryTargetRequirement.Hostile };
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidateEffectTargetCompatibility(state, issues);
        Assert.IsTrue(issues.Count > 0);
    }

    [Test]
    public static void ValidateEffectParameters_ZeroDamage_AddsError()
    {
        var state = new CreatureVariantLabState { effect = LabEffectKind.Damage, value = 0 };
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidateEffectParameters(state, issues);
        bool found = false;
        foreach (var issue in issues)
            if (issue.severity == ValidationSeverity.Error && issue.message.Contains("value greater than 0"))
                found = true;
        Assert.IsTrue(found);
    }

    [Test]
    public static void ValidateModifierParameters_Persistent_AddsBlockingError()
    {
        var state = new CreatureVariantLabState { modifier = LabModifierKind.Persistent };
        var issues = new List<ValidationIssue>();
        SkillCompositionParameterValidator.ValidateModifierParameters(state, issues);
        bool found = false;
        foreach (var issue in issues)
            if (issue.severity == ValidationSeverity.Error && issue.message.Contains("no runtime implementation"))
                found = true;
        Assert.IsTrue(found);
    }
}
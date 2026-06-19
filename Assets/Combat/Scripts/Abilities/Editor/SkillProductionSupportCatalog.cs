using System.Collections.Generic;

public enum SkillProductionSupportStatus
{
    ProductionSupported,
    RuntimePartial,
    ToolOnly,
    Experimental,
    Deprecated,
    MissingRuntime
}

public static class SkillProductionSupportCatalog
{
    public static SkillProductionSupportStatus GetEffectStatus(LabEffectKind effect)
    {
        return effect switch
        {
            LabEffectKind.Damage => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Heal => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Shield => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Stun => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Slow => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.PoisonBurn => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Haste => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.StrengthBuff => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.HealOverTime => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Knockback => SkillProductionSupportStatus.ProductionSupported,
            LabEffectKind.Summon => SkillProductionSupportStatus.Experimental,
            LabEffectKind.Taunt => SkillProductionSupportStatus.RuntimePartial,
            LabEffectKind.Blind => SkillProductionSupportStatus.RuntimePartial,
            _ => SkillProductionSupportStatus.Experimental
        };
    }

    public static SkillProductionSupportStatus GetEffectStatus(SkillEffectKind effect)
    {
        return effect switch
        {
            SkillEffectKind.Damage => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Heal => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Shield => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Haste => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.StrengthBuff => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Slow => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Stun => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.PoisonBurn => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Knockback => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Buff => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Debuff => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.StatModifierDebuff => SkillProductionSupportStatus.ProductionSupported,
            SkillEffectKind.Summon => SkillProductionSupportStatus.Experimental,
            SkillEffectKind.Taunt => SkillProductionSupportStatus.RuntimePartial,
            SkillEffectKind.Blind => SkillProductionSupportStatus.RuntimePartial,
            _ => SkillProductionSupportStatus.Experimental
        };
    }

    public static SkillProductionSupportStatus GetModifierStatus(LabModifierKind modifier)
    {
        return modifier switch
        {
            LabModifierKind.None => SkillProductionSupportStatus.ProductionSupported,
            LabModifierKind.Splash => SkillProductionSupportStatus.ProductionSupported,
            LabModifierKind.Penetrating => SkillProductionSupportStatus.ProductionSupported,
            LabModifierKind.Bounce => SkillProductionSupportStatus.RuntimePartial,
            LabModifierKind.Explosive => SkillProductionSupportStatus.RuntimePartial,
            LabModifierKind.Persistent => SkillProductionSupportStatus.MissingRuntime,
            LabModifierKind.Periodic => SkillProductionSupportStatus.MissingRuntime,
            _ => SkillProductionSupportStatus.Experimental
        };
    }

    public static SkillProductionSupportStatus GetModifierStatus(SkillModifierKind modifier)
    {
        return modifier switch
        {
            SkillModifierKind.Splash => SkillProductionSupportStatus.ProductionSupported,
            SkillModifierKind.Penetrating => SkillProductionSupportStatus.ProductionSupported,
            SkillModifierKind.Bounce => SkillProductionSupportStatus.RuntimePartial,
            SkillModifierKind.Explosive => SkillProductionSupportStatus.RuntimePartial,
            SkillModifierKind.Persistent => SkillProductionSupportStatus.MissingRuntime,
            SkillModifierKind.Periodic => SkillProductionSupportStatus.MissingRuntime,
            SkillModifierKind.Cumulative => SkillProductionSupportStatus.MissingRuntime,
            SkillModifierKind.Expandable => SkillProductionSupportStatus.MissingRuntime,
            _ => SkillProductionSupportStatus.Experimental
        };
    }

    public static bool IsProductionSupported(LabEffectKind effect)
    {
        return GetEffectStatus(effect) == SkillProductionSupportStatus.ProductionSupported;
    }

    public static bool IsProductionSupported(LabModifierKind modifier)
    {
        return GetModifierStatus(modifier) == SkillProductionSupportStatus.ProductionSupported;
    }

    public static string GetDisplayLabel(LabEffectKind effect)
    {
        return $"{effect} [{GetEffectStatus(effect)}]";
    }

    public static string GetDisplayLabel(LabModifierKind modifier)
    {
        return modifier == LabModifierKind.None ? "None" : $"{modifier} [{GetModifierStatus(modifier)}]";
    }

    public static string GetProductionBlockReason(CreatureVariantLabState state)
    {
        if (state == null)
            return "Production save blocked: no composition state is available.";

        if (state.primaryTarget == PrimaryTargetRequirement.GroundCell)
            return "Production save blocked: GroundCell targeting is experimental and not allowed in production variants.";

        SkillProductionSupportStatus effectStatus = GetEffectStatus(state.effect);
        if (effectStatus != SkillProductionSupportStatus.ProductionSupported)
            return $"Production save blocked: effect {state.effect} is {effectStatus}. {GetEffectStatusExplanation(state.effect)}";

        SkillProductionSupportStatus modifierStatus = GetModifierStatus(state.modifier);
        if (modifierStatus != SkillProductionSupportStatus.ProductionSupported)
            return $"Production save blocked: modifier {state.modifier} is {modifierStatus}. {GetModifierStatusExplanation(state.modifier)}";

        return null;
    }

    public static List<string> GetProductionSupportIssues(SkillData skill)
    {
        var issues = new List<string>();
        if (skill == null)
            return issues;

        if (skill.PrimaryTargetRequirement == PrimaryTargetRequirement.GroundCell)
            issues.Add("PrimaryTargetRequirement GroundCell is experimental and not production-supported.");

        SkillCompositionEffect[] effects = skill.CompositionEffects;
        if (effects != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectKind effectKind = effects[i].EffectKind;
                SkillProductionSupportStatus status = GetEffectStatus(effectKind);
                if (status != SkillProductionSupportStatus.ProductionSupported)
                    issues.Add($"Effect {effectKind} is {status}. {GetEffectStatusExplanation(effectKind)}");
            }
        }

        SkillModifierKind[] modifiers = skill.CompositionModifierKinds;
        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Length; i++)
            {
                SkillModifierKind modifierKind = modifiers[i];
                SkillProductionSupportStatus status = GetModifierStatus(modifierKind);
                if (status != SkillProductionSupportStatus.ProductionSupported)
                    issues.Add($"Modifier {modifierKind} is {status}. {GetModifierStatusExplanation(modifierKind)}");
            }
        }

        return issues;
    }

    public static string GetEffectStatusExplanation(LabEffectKind effect)
    {
        return GetEffectStatusExplanation(SkillCompositionMapper.ToSkillEffectKind(effect));
    }

    public static string GetModifierStatusExplanation(LabModifierKind modifier)
    {
        return modifier == LabModifierKind.None
            ? "No modifier selected."
            : GetModifierStatusExplanation(SkillCompositionMapper.ToSkillModifierKind(modifier));
    }

    public static string GetEffectStatusExplanation(SkillEffectKind effect)
    {
        return effect switch
        {
            SkillEffectKind.Summon => "Summon runtime exists for debug/testing flows but is not approved for production assets.",
            SkillEffectKind.Taunt => "Taunt works through forced targeting, but it is still considered partial for production authoring.",
            SkillEffectKind.Blind => "Blind relies on generic accuracy debuffs and is still considered partial for production authoring.",
            _ => "This effect is not approved for production authoring."
        };
    }

    public static string GetModifierStatusExplanation(SkillModifierKind modifier)
    {
        return modifier switch
        {
            SkillModifierKind.Bounce => "Bounce has runtime support, but it remains partial and is blocked for production assets.",
            SkillModifierKind.Explosive => "Explosive has runtime support, but it remains partial and is blocked for production assets.",
            SkillModifierKind.Persistent => "Persistent has no production runtime in SkillCompositionRuntimeExecutor.",
            SkillModifierKind.Periodic => "Periodic has no production runtime in SkillCompositionRuntimeExecutor.",
            SkillModifierKind.Cumulative => "Cumulative has no production runtime in SkillCompositionRuntimeExecutor.",
            SkillModifierKind.Expandable => "Expandable has no production runtime in SkillCompositionRuntimeExecutor.",
            _ => "This modifier is not approved for production authoring."
        };
    }
}

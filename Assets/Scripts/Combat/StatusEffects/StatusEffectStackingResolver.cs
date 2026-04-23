using System.Collections.Generic;

public enum StatusEffectStackResolution
{
    AddNewInstance,
    RefreshExisting,
    AddStackToExisting,
    ReplaceExisting,
    Ignore
}

public static class StatusEffectStackingResolver
{
    public static StatusEffectStackResolution Resolve(
        StatusEffectApplication application,
        List<ActiveStatusEffect> activeEffects,
        out ActiveStatusEffect existingEffect)
    {
        existingEffect = FindMatchingEffect(application, activeEffects);
        if (existingEffect == null)
            return StatusEffectStackResolution.AddNewInstance;

        StatusEffectDefinition definition = application.Definition;
        return definition.StackingMode switch
        {
            EffectStackingMode.AddStack => StatusEffectStackResolution.AddStackToExisting,
            EffectStackingMode.IndependentInstance => StatusEffectStackResolution.AddNewInstance,
            EffectStackingMode.ReplaceByStronger => ResolveReplaceByStronger(application, existingEffect),
            EffectStackingMode.IgnoreIfSameSource => ResolveIgnoreIfSameSource(application, existingEffect),
            _ => StatusEffectStackResolution.RefreshExisting
        };
    }

    private static ActiveStatusEffect FindMatchingEffect(StatusEffectApplication application, List<ActiveStatusEffect> activeEffects)
    {
        if (activeEffects == null)
            return null;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect candidate = activeEffects[i];
            if (candidate == null || candidate.Definition != application.Definition)
                continue;

            return candidate;
        }

        return null;
    }

    private static StatusEffectStackResolution ResolveReplaceByStronger(
        StatusEffectApplication application,
        ActiveStatusEffect existingEffect)
    {
        if (existingEffect == null)
            return StatusEffectStackResolution.AddNewInstance;

        float incomingStrength = application.Definition != null ? application.Definition.Strength : 0f;
        float existingStrength = existingEffect.Definition != null ? existingEffect.Definition.Strength : 0f;
        return incomingStrength >= existingStrength
            ? StatusEffectStackResolution.ReplaceExisting
            : StatusEffectStackResolution.Ignore;
    }

    private static StatusEffectStackResolution ResolveIgnoreIfSameSource(
        StatusEffectApplication application,
        ActiveStatusEffect existingEffect)
    {
        if (existingEffect == null)
            return StatusEffectStackResolution.AddNewInstance;

        return ReferenceEquals(existingEffect.SourceUnit, application.SourceUnit)
            ? StatusEffectStackResolution.Ignore
            : StatusEffectStackResolution.AddNewInstance;
    }
}

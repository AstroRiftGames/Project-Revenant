using UnityEngine;

public class FusionVisualResolver
{
    private readonly FusionVisualConfig _config;

    public FusionVisualResolver(FusionVisualConfig config)
    {
        _config = config;
    }

    public Sprite ResolveSprite(UnitFaction a, UnitFaction b, UnitFaction dominant)
    {
        UnitFaction secondary = (dominant == a) ? b : a;

        if (_config.CombinationOverrides != null)
        {
            foreach (FusionResultProfile profile in _config.CombinationOverrides)
            {
                if (profile.MatchesCombinations(dominant, secondary))
                {
                    return profile.CreatureSprite;
                }
            }
        }

        if (_config.DominantFallbacks != null)
        {
            foreach (FactionVisualFallback fallback in _config.DominantFallbacks)
            {
                if (fallback.UnitFaction == dominant)
                {
                    return fallback.FallbackSprite;
                }
            }
        }

        return null;
    }
}


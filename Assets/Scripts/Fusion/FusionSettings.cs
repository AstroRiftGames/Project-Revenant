using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFusionSettings", menuName = "Fusion/Settings")]
public class FusionSettings : ScriptableObject
{
    [SerializeField] private FusionCompatibilityMatrix _compatibilityMatrix;
    [SerializeField] private List<FusionResultProfile> _resultProfiles = new List<FusionResultProfile>();

    public bool AreFactionsCompatible(Faction factionA, Faction factionB)
    {
        if (_compatibilityMatrix == null)
        {
            return false;
        }

        return _compatibilityMatrix.IsCompatible(factionA, factionB);
    }

    public Faction GetDominantFaction(Faction factionA, Faction factionB)
    {
        if (_compatibilityMatrix == null)
        {
            return Faction.None;
        }

        return _compatibilityMatrix.GetDominantFaction(factionA, factionB);
    }

    public FusionResultProfile GetFusionResult(Faction factionA, Faction factionB)
    {
        if (!AreFactionsCompatible(factionA, factionB))
        {
            return null;
        }

        Faction dominant = GetDominantFaction(factionA, factionB);
        Faction secondary = (dominant == factionA) ? factionB : factionA;

        foreach (FusionResultProfile profile in _resultProfiles)
        {
            if (profile.MatchesCombinations(dominant, secondary))
            {
                return profile;
            }
        }

        return null;
    }
}

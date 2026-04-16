using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct FactionPair : IEquatable<FactionPair>
{
    public UnitFaction FactionA;
    public UnitFaction FactionB;

    public FactionPair(UnitFaction factionA, UnitFaction factionB)
    {
        FactionA = factionA;
        FactionB = factionB;
    }

    public bool Equals(FactionPair other)
    {
        return (FactionA == other.FactionA && FactionB == other.FactionB) ||
               (FactionA == other.FactionB && FactionB == other.FactionA);
    }

    public override bool Equals(object obj)
    {
        return obj is FactionPair other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hashA = (int)FactionA;
        int hashB = (int)FactionB;
        return hashA < hashB ? HashCode.Combine(hashA, hashB) : HashCode.Combine(hashB, hashA);
    }
}

[Serializable]
public struct CompatibilityMapping
{
    public FactionPair Factions;
    public bool IsCompatible;
    public UnitFaction DominantFaction;
}

[CreateAssetMenu(fileName = "NewFusionCompatibilityMatrix", menuName = "Fusion/Compatibility Matrix")]
public class FusionCompatibilityMatrix : ScriptableObject
{
    [SerializeField] private List<CompatibilityMapping> _compatibilityMappings = new List<CompatibilityMapping>();

    public bool IsCompatible(UnitFaction factionA, UnitFaction factionB)
    {
        FactionPair pairToFind = new FactionPair(factionA, factionB);

        foreach (CompatibilityMapping mapping in _compatibilityMappings)
        {
            if (mapping.Factions.Equals(pairToFind))
            {
                return mapping.IsCompatible;
            }
        }

        return false;
    }

    public UnitFaction GetDominantFaction(UnitFaction factionA, UnitFaction factionB)
    {
        FactionPair pairToFind = new FactionPair(factionA, factionB);

        foreach (CompatibilityMapping mapping in _compatibilityMappings)
        {
            if (mapping.Factions.Equals(pairToFind) && mapping.IsCompatible)
            {
                return mapping.DominantFaction;
            }
        }

        return UnitFaction.None;
    }
}


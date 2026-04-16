using UnityEngine;

[CreateAssetMenu(fileName = "NewFusionSettings", menuName = "Fusion/Settings")]
public class FusionSettings : ScriptableObject
{
    [SerializeField] private FusionCompatibilityMatrix _compatibilityMatrix;

    public bool AreFactionsCompatible(UnitFaction factionA, UnitFaction factionB)
    {
        if (_compatibilityMatrix == null)
        {
            return false;
        }

        return _compatibilityMatrix.IsCompatible(factionA, factionB);
    }

    public UnitFaction GetDominantFaction(UnitFaction factionA, UnitFaction factionB)
    {
        if (_compatibilityMatrix == null)
        {
            return UnitFaction.None;
        }

        return _compatibilityMatrix.GetDominantFaction(factionA, factionB);
    }
}


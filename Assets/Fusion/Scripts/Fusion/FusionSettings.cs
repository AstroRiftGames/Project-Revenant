using UnityEngine;

[CreateAssetMenu(fileName = "NewFusionSettings", menuName = "Fusion/Settings")]
public class FusionSettings : ScriptableObject
{
    [SerializeField] private FusionCompatibilityMatrix _compatibilityMatrix;

    // ── Compatibilidad a nivel de facción (legacy) ──────────────────

    public bool AreFactionsCompatible(UnitFaction factionA, UnitFaction factionB)
    {
        if (_compatibilityMatrix == null)
            return false;

        return _compatibilityMatrix.IsCompatible(factionA, factionB);
    }

    public UnitFaction GetDominantFaction(UnitFaction factionA, UnitFaction factionB)
    {
        if (_compatibilityMatrix == null)
            return UnitFaction.None;

        return _compatibilityMatrix.GetDominantFaction(factionA, factionB);
    }

    // ── Compatibilidad a nivel de facción + rol ─────────────────────

    /// <summary>
    /// Verifica si el par facción+rol de cada criatura satisface la regla de compatibilidad.
    /// </summary>
    public bool AreRolesCompatible(
        UnitFaction factionA, UnitRole roleA,
        UnitFaction factionB, UnitRole roleB)
    {
        if (_compatibilityMatrix == null)
            return false;

        return _compatibilityMatrix.AreRolesCompatible(factionA, roleA, factionB, roleB);
    }

    /// <summary>
    /// Retorna el rol de la criatura no dominante, que será el rol del resultado.
    /// </summary>
    public UnitRole GetResultRole(
        UnitFaction dominantFaction,
        UnitFaction factionA, UnitRole roleA,
        UnitFaction factionB, UnitRole roleB)
    {
        if (_compatibilityMatrix == null)
            return default;

        return _compatibilityMatrix.GetResultRole(dominantFaction, factionA, roleA, factionB, roleB);
    }
}

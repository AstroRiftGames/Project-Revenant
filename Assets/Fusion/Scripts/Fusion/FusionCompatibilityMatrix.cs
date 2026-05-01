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
    public FactionPair  Factions;
    /// <summary>Rol requerido de FactionA para que la fusión sea válida.</summary>
    public UnitRole     RoleOfFactionA;
    /// <summary>Rol requerido de FactionB para que la fusión sea válida.</summary>
    public UnitRole     RoleOfFactionB;
    public bool         IsCompatible;
    /// <summary>La facción cuya identidad visual/facción hereda la criatura resultante.</summary>
    public UnitFaction  DominantFaction;
}

[CreateAssetMenu(fileName = "NewFusionCompatibilityMatrix", menuName = "Fusion/Compatibility Matrix")]
public class FusionCompatibilityMatrix : ScriptableObject
{
    [SerializeField] private List<CompatibilityMapping> _compatibilityMappings = new List<CompatibilityMapping>();

    // ──────────────────────────────────────────────────────────────
    //  Compatibilidad a nivel de FACCIÓN (mantenido por compatibilidad)
    // ──────────────────────────────────────────────────────────────

    public bool IsCompatible(UnitFaction factionA, UnitFaction factionB)
    {
        FactionPair pairToFind = new FactionPair(factionA, factionB);

        foreach (CompatibilityMapping mapping in _compatibilityMappings)
        {
            if (mapping.Factions.Equals(pairToFind))
                return mapping.IsCompatible;
        }

        return false;
    }

    public UnitFaction GetDominantFaction(UnitFaction factionA, UnitFaction factionB)
    {
        FactionPair pairToFind = new FactionPair(factionA, factionB);

        foreach (CompatibilityMapping mapping in _compatibilityMappings)
        {
            if (mapping.Factions.Equals(pairToFind) && mapping.IsCompatible)
                return mapping.DominantFaction;
        }

        return UnitFaction.None;
    }

    // ──────────────────────────────────────────────────────────────
    //  Compatibilidad a nivel de FACCIÓN + ROL
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica si el par (factionA, roleA) es compatible con (factionB, roleB)
    /// según la regla definida en el mapping.
    /// </summary>
    public bool AreRolesCompatible(
        UnitFaction factionA, UnitRole roleA,
        UnitFaction factionB, UnitRole roleB)
    {
        FactionPair pairToFind = new FactionPair(factionA, factionB);

        foreach (CompatibilityMapping mapping in _compatibilityMappings)
        {
            if (!mapping.Factions.Equals(pairToFind) || !mapping.IsCompatible)
                continue;

            // El par puede llegar en cualquier orden; hay que comprobar ambas orientaciones.
            bool directMatch   = mapping.Factions.FactionA == factionA
                                 && roleA == mapping.RoleOfFactionA
                                 && roleB == mapping.RoleOfFactionB;

            bool reverseMatch  = mapping.Factions.FactionA == factionB
                                 && roleB == mapping.RoleOfFactionA
                                 && roleA == mapping.RoleOfFactionB;

            if (directMatch || reverseMatch)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Retorna el rol de la criatura NO dominante (el rol que heredará el resultado).
    /// Requiere haber validado la compatibilidad previamente.
    /// </summary>
    public UnitRole GetResultRole(
        UnitFaction dominantFaction,
        UnitFaction factionA, UnitRole roleA,
        UnitFaction factionB, UnitRole roleB)
    {
        // La criatura resultante toma el rol de quien NO es dominante.
        return dominantFaction == factionA ? roleB : roleA;
    }
}

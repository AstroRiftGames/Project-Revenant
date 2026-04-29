using System.Collections.Generic;
using UnityEngine;

namespace Altar.Data
{
    [System.Serializable]
    public class SacrificeRequirement
    {
        [Tooltip("If true, requires a specific UnitData. If false, uses Faction/Role filters.")]
        public bool requiresSpecificUnit;

        [Header("Specific Unit Filter")]
        [Tooltip("The exact UnitData required if 'requiresSpecificUnit' is true.")]
        public UnitData specificUnit;

        [Header("General Filters (if not specific)")]
        public bool anyFaction;
        [Tooltip("The required Faction if 'anyFaction' is false.")]
        public UnitFaction requiredFaction;

        public bool anyRole;
        [Tooltip("The required Role if 'anyRole' is false.")]
        public UnitRole requiredRole;

        [Header("Amount")]
        [Min(1)]
        [Tooltip("How many units matching this requirement are needed.")]
        public int amount = 1;
    }

    [CreateAssetMenu(fileName = "NewAltarSacrifice", menuName = "Altar/Sacrifice Data")]
    public class AltarSacrificeData : ScriptableObject
    {
        [Tooltip("A name to identify this sacrifice (e.g., 'The Orc Warlord Ritual').")]
        public string sacrificeName;

        [Tooltip("The list of requirements that must be met to perform this sacrifice.")]
        public List<SacrificeRequirement> requirements = new List<SacrificeRequirement>();

        [Tooltip("The unit that will be rewarded upon completing this sacrifice.")]
        public UnitData rewardUnit;
    }
}

using System.Collections.Generic;
using UnityEngine;
using Altar.Data;

namespace Altar.Core
{
    public class AltarService
    {
        /// <summary>
        /// Checks if a single PartyMemberData matches a single SacrificeRequirement.
        /// </summary>
        public static bool MatchesRequirement(PartyMemberData member, SacrificeRequirement req)
        {
            if (member == null) return false;

            if (req.requiresSpecificUnit)
            {
                // Verifica si es exactamente el UnitData requerido. 
                // Asumiendo que UnitDefinition tiene los datos base.
                if (req.specificUnit == null) return false;
                return member.UnitDefinition == req.specificUnit || (member.UnitDefinition != null && member.UnitDefinition.unitId == req.specificUnit.unitId);
            }
            else
            {
                bool factionMatches = req.anyFaction || (member.RuntimeFaction == req.requiredFaction);
                bool roleMatches = req.anyRole || (member.Role == req.requiredRole);

                return factionMatches && roleMatches;
            }
        }

        /// <summary>
        /// Validates if the selected members exactly fulfill the sacrifice requirements.
        /// Returns true if the selected members are enough and valid.
        /// </summary>
        public bool ValidateSacrifice(AltarSacrificeData sacrifice, List<PartyMemberData> selectedMembers)
        {
            if (sacrifice == null || sacrifice.requirements == null) return false;
            
            // Creamos una copia de los miembros seleccionados para ir consumiéndolos lógicamente
            List<PartyMemberData> availableToMatch = new List<PartyMemberData>(selectedMembers);

            foreach (var req in sacrifice.requirements)
            {
                int matchedCount = 0;

                for (int i = availableToMatch.Count - 1; i >= 0; i--)
                {
                    if (MatchesRequirement(availableToMatch[i], req))
                    {
                        matchedCount++;
                        availableToMatch.RemoveAt(i);

                        if (matchedCount >= req.amount)
                            break;
                    }
                }

                if (matchedCount < req.amount)
                {
                    // No se cumplió un requerimiento
                    return false;
                }
            }

            // Opcional: Si sobran miembros seleccionados que no fueron necesarios, podríamos devolver falso
            // indicando que seleccionó criaturas de más. Asumiremos que debe ser exacto.
            if (availableToMatch.Count > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Executes the sacrifice, dismissing the units from the party and adding the reward.
        /// </summary>
        public AltarResult ExecuteSacrifice(AltarSacrificeData sacrifice, List<PartyMemberData> selectedMembers)
        {
            if (!ValidateSacrifice(sacrifice, selectedMembers))
            {
                return AltarResult.Failure("The selected units do not match the sacrifice requirements.");
            }

            if (NecromancerParty.Instance == null)
            {
                return AltarResult.Failure("System error: NecromancerParty not found.");
            }

            if (sacrifice.rewardUnit == null)
            {
                return AltarResult.Failure("Configuration error: Sacrifice has no reward unit.");
            }

            // Dismiss the sacrificed units
            foreach (var member in selectedMembers)
            {
                NecromancerParty.Instance.DismissMember(member);
            }

            // Otorga la recompensa
            // Creamos una nueva instancia de UnitData basándonos en la recompensa para no modificar el asset base.
            UnitData rewardInstance = ScriptableObject.Instantiate(sacrifice.rewardUnit);
            rewardInstance.unitId = System.Guid.NewGuid().ToString(); // Asignar un ID único
            
            bool added = NecromancerParty.Instance.TryAddMember(rewardInstance);
            
            if (!added)
            {
                // Si la party está llena, se podría manejar de forma especial, pero aquí devolvemos fallo o éxito parcial.
                // Idealmente el sistema valida antes si hay espacio, o la UI no deja hacerlo si está llena.
                // Como se liberaron espacios al despedir miembros, normalmente siempre habrá espacio.
                return AltarResult.Failure("Failed to add the rewarded creature to the party.");
            }

            return AltarResult.Success(sacrifice.rewardUnit);
        }
    }
}

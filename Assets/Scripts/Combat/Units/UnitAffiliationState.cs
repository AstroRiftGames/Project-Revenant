using UnityEngine;

[DisallowMultipleComponent]
public class UnitAffiliationState : MonoBehaviour
{
    public UnitTeam Team { get; private set; } = UnitTeam.Enemy;
    public UnitFaction Faction { get; private set; }

    public void Initialize(UnitData unitData)
    {
        Team = unitData != null ? unitData.team : UnitTeam.Enemy;
        Faction = unitData != null ? unitData.faction : default;
    }

    public void SetAffiliation(UnitTeam team, UnitFaction faction)
    {
        Team = team;
        Faction = faction;
    }
}

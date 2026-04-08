using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(UnitDeathHandler))]
[RequireComponent(typeof(UnitAffiliationState))]
public class UnitRecruitmentReviver : MonoBehaviour
{
    [SerializeField] private int _minimumRecruitHealth = 1;

    private Unit _unit;
    private UnitDeathHandler _deathHandler;
    private UnitAffiliationState _affiliationState;
    private PartyMemberLink _partyMemberLink;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _deathHandler = GetComponent<UnitDeathHandler>();
        _affiliationState = GetComponent<UnitAffiliationState>();
        _partyMemberLink = GetComponent<PartyMemberLink>();
    }

    public void RestoreRecruitedUnit(PartyMemberData member)
    {
        if (member == null || _unit == null || _deathHandler == null || _affiliationState == null)
            return;

        _partyMemberLink ??= GetComponent<PartyMemberLink>() ?? gameObject.AddComponent<PartyMemberLink>();
        _partyMemberLink.Initialize(member.PartyMemberId, true);
        _affiliationState.SetAffiliation(member.RuntimeTeam, member.RuntimeFaction);

        int restoredHealth = Mathf.Clamp(member.CurrentHealth, Mathf.Max(1, _minimumRecruitHealth), _unit.MaxHealth);
        _deathHandler.RestoreOperationalState(restoredHealth);
    }
}

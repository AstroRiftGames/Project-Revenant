using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PartyDefeatDetector : MonoBehaviour
{
    [SerializeField] private NecromancerParty _party;

    public event Action PartyDefeated;

    public void Configure(NecromancerParty party)
    {
        _party = party;
    }

    private void OnEnable()
    {
        LifeController.OnUnitDied += HandleUnitDied;
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleUnitDied;
    }

    private void HandleUnitDied(Unit unit)
    {
        if (!IsNecromancerPartyMember(unit, out PartyMemberLink memberLink))
            return;

        if (HasAlivePartyMembersExcept(memberLink.PartyMemberId))
            return;

        PartyDefeated?.Invoke();
    }

    private bool HasAlivePartyMembersExcept(string excludedPartyMemberId)
    {
        if (_party == null)
            return false;

        IReadOnlyList<PartyMemberData> members = _party.Members;
        for (int i = 0; i < members.Count; i++)
        {
            PartyMemberData member = members[i];
            if (member == null || !member.IsAlive)
                continue;

            if (member.PartyMemberId == excludedPartyMemberId)
                continue;

            return true;
        }

        return false;
    }

    private static bool IsNecromancerPartyMember(Unit unit, out PartyMemberLink memberLink)
    {
        memberLink = null;
        if (unit == null)
            return false;

        memberLink = unit.GetComponent<PartyMemberLink>();
        return memberLink != null && memberLink.IsFromNecromancerParty;
    }
}

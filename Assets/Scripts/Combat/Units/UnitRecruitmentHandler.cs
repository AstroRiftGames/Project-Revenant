using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(UnitDeathHandler))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(UnitRecruitmentReviver))]
public class UnitRecruitmentHandler : MonoBehaviour
{
    private Unit _unit;
    private RecruitableUnitState _recruitableState;
    private UnitRecruitmentReviver _reviver;
    private NecromancerPartyContext _partyContext;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _reviver = GetComponent<UnitRecruitmentReviver>();
        _partyContext = NecromancerPartyContext.Current;
    }

    public void Configure(NecromancerPartyContext partyContext)
    {
        _partyContext = partyContext;
    }

    public bool AttemptRecruitment()
    {
        if (_unit == null || _recruitableState == null || _reviver == null)
            return false;

        if (_recruitableState.CurrentState != UnitLifecycleState.Recruitable)
            return false;

        _partyContext ??= NecromancerPartyContext.Current;
        if (_partyContext == null)
            return false;

        if (!_partyContext.TryRecruitUnit(_unit, out PartyMemberData member))
            return false;

        _reviver.RestoreRecruitedUnit(member);
        _partyContext.RegisterDeployedUnit(gameObject, member.PartyMemberId);
        return true;
    }
}

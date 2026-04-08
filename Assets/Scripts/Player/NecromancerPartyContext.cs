using UnityEngine;

[DisallowMultipleComponent]
public class NecromancerPartyContext : MonoBehaviour
{
    public static NecromancerPartyContext Current { get; private set; }

    [SerializeField] private NecromancerParty _party;
    [SerializeField] private RoomPartySpawner _partySpawner;

    public NecromancerParty Party => _party;
    public RoomPartySpawner PartySpawner => _partySpawner;

    private void OnEnable()
    {
        Current = this;
    }

    private void OnDisable()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
    }

    public void Configure(NecromancerParty party, RoomPartySpawner partySpawner)
    {
        _party = party;
        _partySpawner = partySpawner;
    }

    public bool TryRecruitUnit(Unit unit, out PartyMemberData member)
    {
        member = null;

        if (_party == null)
            return false;

        return _party.TryRecruitUnit(unit, out member);
    }

    public void RegisterDeployedUnit(GameObject instance, string partyMemberId)
    {
        _partySpawner?.TrackDeployment(instance, partyMemberId);
    }
}

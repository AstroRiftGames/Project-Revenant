using UnityEngine;

public class PartyMemberLink : MonoBehaviour
{
    [SerializeField] private string _partyMemberId;
    [SerializeField] private bool _isFromNecromancerParty;

    public string PartyMemberId => _partyMemberId;
    public bool IsFromNecromancerParty => _isFromNecromancerParty;

    public void Initialize(string partyMemberId, bool isFromNecromancerParty)
    {
        _partyMemberId = partyMemberId;
        _isFromNecromancerParty = isFromNecromancerParty;
    }
}

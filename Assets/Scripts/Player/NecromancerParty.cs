using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class PartyMemberData
{
    public string PartyMemberId;
    public UnitData UnitData;
    public bool IsAlive = true;
    public int FormationIndex;
    public bool IsDeployed;
    public int CurrentHealth;
}

public class NecromancerParty : MonoBehaviour
{
    [SerializeField] private int _maxPartyMembers = 3;
    [SerializeField] private bool _showDebugOverlay;
    [SerializeField] private List<UnitData> _startingMembers = new();
    [SerializeField] private List<PartyMemberData> _members = new();

    public int MaxPartyMembers => _maxPartyMembers;
    public int SlotsUsed => _members.Count;
    public IReadOnlyList<PartyMemberData> Members => _members;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SeedStartingPartyIfNeeded();
    }

    private void OnEnable()
    {
        LifeController.OnUnitDied += HandleUnitDied;
        LifeController.OnHealthChanged += HandleUnitHealthChanged;
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleUnitDied;
        LifeController.OnHealthChanged -= HandleUnitHealthChanged;
    }

    public void Configure(List<UnitData> startingMembers, int maxPartyMembers, bool showDebugOverlay)
    {
        _maxPartyMembers = Mathf.Max(1, maxPartyMembers);
        _showDebugOverlay = showDebugOverlay;

        if (startingMembers != null && startingMembers.Count > 0)
            _startingMembers = new List<UnitData>(startingMembers);

        SeedStartingPartyIfNeeded();
    }

    public IEnumerable<PartyMemberData> GetDeployableMembers()
    {
        return _members
            .Where(member => member != null && member.IsAlive && member.UnitData != null)
            .OrderBy(member => member.FormationIndex)
            .Take(_maxPartyMembers);
    }

    public bool TryGetMember(string partyMemberId, out PartyMemberData member)
    {
        member = _members.FirstOrDefault(candidate => candidate != null && candidate.PartyMemberId == partyMemberId);
        return member != null;
    }

    public void ClearDeploymentFlags()
    {
        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i] == null)
                continue;

            _members[i].IsDeployed = false;
        }
    }

    public void MarkDeployed(string partyMemberId, bool isDeployed)
    {
        if (TryGetMember(partyMemberId, out PartyMemberData member))
            member.IsDeployed = isDeployed;
    }

    public bool TryAddMember(UnitData unitData, int formationIndex = -1)
    {
        if (unitData == null || _members.Count >= _maxPartyMembers)
            return false;

        int nextFormationIndex = formationIndex >= 0 ? formationIndex : _members.Count;
        _members.Add(CreateMember(unitData, nextFormationIndex));
        return true;
    }

    private void SeedStartingPartyIfNeeded()
    {
        if (_members.Count > 0 || _startingMembers == null || _startingMembers.Count == 0)
            return;

        for (int i = 0; i < _startingMembers.Count && _members.Count < _maxPartyMembers; i++)
        {
            UnitData unitData = _startingMembers[i];
            if (unitData == null)
                continue;

            _members.Add(CreateMember(unitData, i));
        }
    }

    private PartyMemberData CreateMember(UnitData unitData, int formationIndex)
    {
        return new PartyMemberData
        {
            PartyMemberId = Guid.NewGuid().ToString("N"),
            UnitData = unitData,
            IsAlive = true,
            FormationIndex = formationIndex,
            IsDeployed = false,
            CurrentHealth = unitData != null && unitData.stats != null ? Mathf.Max(1, unitData.stats.maxHealth) : 1
        };
    }

    private void HandleUnitDied(Unit unit)
    {
        if (unit == null)
            return;

        PartyMemberLink link = unit.GetComponent<PartyMemberLink>();
        if (link == null || !link.IsFromNecromancerParty)
            return;

        if (!TryGetMember(link.PartyMemberId, out PartyMemberData member))
            return;

        member.IsAlive = false;
        member.IsDeployed = false;
        member.CurrentHealth = 0;
    }

    private void HandleUnitHealthChanged(Unit unit)
    {
        if (unit == null)
            return;

        PartyMemberLink link = unit.GetComponent<PartyMemberLink>();
        if (link == null || !link.IsFromNecromancerParty)
            return;

        if (!TryGetMember(link.PartyMemberId, out PartyMemberData member))
            return;

        member.CurrentHealth = Mathf.Max(0, unit.CurrentHealth);
    }

    private void OnGUI()
    {
        if (!_showDebugOverlay)
            return;

        GUILayout.BeginArea(new Rect(10f, 10f, 360f, 400f), GUI.skin.box);
        GUILayout.Label($"Necromancer Party {SlotsUsed}/{_maxPartyMembers}");

        for (int i = 0; i < _members.Count; i++)
        {
            PartyMemberData member = _members[i];
            if (member == null)
                continue;

            string unitName = member.UnitData != null ? member.UnitData.displayName : "Missing UnitData";
            GUILayout.Label(
                $"[{member.FormationIndex}] {unitName} | HP {member.CurrentHealth} | " +
                $"{(member.IsAlive ? "Alive" : "Dead")} | {(member.IsDeployed ? "Deployed" : "Idle")}");
        }

        GUILayout.EndArea();
    }
}

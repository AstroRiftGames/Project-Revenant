using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Selection.Interfaces;

[Serializable]
public class PartyMemberData : ISelectable, ICharacterStatsProvider
{
    public string PartyMemberId;
    public UnitData UnitDefinition;
    public UnitTeam RuntimeTeam;
    public UnitFaction RuntimeFaction;
    public bool IsAlive = true;
    public int FormationIndex;
    public bool IsDeployed;
    public int CurrentHealth;

    // ISelectable logic
    public bool IsSelected => true;
    public GameObject SelectionGameObject => null;
    public ICharacterStatsProvider StatsProvider => this;
    public event Action<ISelectable> OnSelectionInvalidated { add { } remove { } }
    public event Action<ISelectable, bool> OnSelectionStateChanged { add { } remove { } }
    public void Select() { }
    public void Deselect() { }

    // ICharacterStatsProvider logic
    public UnitTeam Team => RuntimeTeam;
    int ICharacterStatsProvider.CurrentHealth => CurrentHealth;
    public int MaxHealth => UnitDefinition != null && UnitDefinition.stats != null ? Mathf.Max(1, UnitDefinition.stats.maxHealth) : 1;
    public UnitRole Role => UnitDefinition != null ? UnitDefinition.role : default;
    public float CurrentAbilityCooldown => 0;
    public float MaxAbilityCooldown => 1;
    public Sprite AbilityIcon => null;
    public Sprite CharacterSprite => UnitDefinition != null ? UnitDefinition.sprite : null;
    public UnitStatsData CoreStats => UnitDefinition != null ? UnitDefinition.stats : null;
}

public class NecromancerParty : MonoBehaviour
{
    public static event Action OnPartyUpdated;
    [SerializeField] private int _maxPartyMembers = 3;
    [SerializeField] private bool _showDebugOverlay;
    [SerializeField] private List<UnitData> _startingMembers = new();
    [SerializeField] private List<PartyMemberData> _members = new();

    public int MaxPartyMembers => _maxPartyMembers;
    public int SlotsUsed => _members.Count;
    public IReadOnlyList<PartyMemberData> Members => _members;

    public static NecromancerParty Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SeedStartingPartyIfNeeded();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        OnPartyUpdated?.Invoke();
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
        SetMaxPartyMembers(maxPartyMembers, notify: false);
        _showDebugOverlay = showDebugOverlay;

        if (startingMembers != null && startingMembers.Count > 0)
            _startingMembers = new List<UnitData>(startingMembers);

        SeedStartingPartyIfNeeded();
    }

    public IEnumerable<PartyMemberData> GetDeployableMembers()
    {
        return _members
            .Where(member => member != null && member.IsAlive && member.UnitDefinition != null)
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
        if (unitData == null || SlotsUsed >= _maxPartyMembers)
            return false;

        int nextFormationIndex = formationIndex >= 0 ? formationIndex : GetNextFormationIndex();
        _members.Add(CreateMember(unitData, nextFormationIndex));
        NormalizeFormationIndices();
        OnPartyUpdated?.Invoke();
        return true;
    }

    public bool TryRecruitUnit(Unit unit, out PartyMemberData member)
    {
        member = null;

        if (unit == null || SlotsUsed >= _maxPartyMembers)
            return false;

        UnitData unitData = unit.GetUnitData();
        if (unitData == null)
            return false;

        PartyMemberLink existingLink = unit.GetComponent<PartyMemberLink>();
        if (existingLink != null && existingLink.IsFromNecromancerParty && TryGetMember(existingLink.PartyMemberId, out member))
            return true;

        int currentHealth = Mathf.Max(1, unit.BaseMaxHealth);
        member = CreateMember(unitData, GetNextFormationIndex(), UnitTeam.NecromancerAlly, unit.Faction, currentHealth);
        member.IsDeployed = true;
        _members.Add(member);
        NormalizeFormationIndices();
        OnPartyUpdated?.Invoke();
        return true;
    }

    private void SeedStartingPartyIfNeeded()
    {
        if (_members.Count > 0 || _startingMembers == null || _startingMembers.Count == 0)
            return;

        for (int i = 0; i < _startingMembers.Count && SlotsUsed < _maxPartyMembers; i++)
        {
            UnitData unitData = _startingMembers[i];
            if (unitData == null)
                continue;

            _members.Add(CreateMember(unitData, i));
        }

        NormalizeFormationIndices();
        OnPartyUpdated?.Invoke();
    }

    public void SetMaxPartyMembers(int maxPartyMembers, bool notify = true)
    {
        int nextMaxPartyMembers = Mathf.Max(1, maxPartyMembers);
        if (_maxPartyMembers == nextMaxPartyMembers)
            return;

        _maxPartyMembers = nextMaxPartyMembers;

        if (notify)
            OnPartyUpdated?.Invoke();
    }

    public void ResetToStartingParty()
    {
        _members.Clear();
        SeedStartingPartyIfNeeded();
    }

    private PartyMemberData CreateMember(UnitData unitData, int formationIndex)
    {
        return CreateMember(
            unitData,
            formationIndex,
            unitData != null ? unitData.team : UnitTeam.Enemy,
            unitData != null ? unitData.faction : default,
            unitData != null && unitData.stats != null ? Mathf.Max(1, unitData.stats.maxHealth) : 1);
    }

    private PartyMemberData CreateMember(UnitData unitData, int formationIndex, UnitTeam team, UnitFaction faction, int currentHealth)
    {
        return new PartyMemberData
        {
            PartyMemberId = Guid.NewGuid().ToString("N"),
            UnitDefinition = unitData,
            RuntimeTeam = team,
            RuntimeFaction = faction,
            IsAlive = true,
            FormationIndex = formationIndex,
            IsDeployed = false,
            CurrentHealth = Mathf.Max(1, currentHealth)
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

        RemoveMember(member);
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

        const float panelWidth = 360f;
        const float panelHeight = 220f;
        float panelX = Screen.width - panelWidth - 10f;
        float panelY = 10f;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight), GUI.skin.box);
        GUILayout.Label($"Necromancer Party {SlotsUsed}/{_maxPartyMembers}");

        List<PartyMemberData> activeMembers = _members
            .Where(member => member != null && member.IsAlive)
            .OrderBy(member => member.FormationIndex)
            .ToList();

        if (activeMembers.Count == 0)
        {
            GUILayout.Label("No active party members.");
        }
        else
        {
            for (int i = 0; i < activeMembers.Count; i++)
            {
                PartyMemberData member = activeMembers[i];

                string unitName = member.UnitDefinition != null ? member.UnitDefinition.displayName : "Missing UnitData";
                GUILayout.Label(
                    $"[{member.FormationIndex}] {unitName} | HP {member.CurrentHealth} | " +
                    $"{(member.IsDeployed ? "Deployed" : "Idle")}");
            }
        }

        GUILayout.EndArea();
    }

    private int GetNextFormationIndex()
    {
        return _members.Count;
    }

    public void DismissMember(PartyMemberData member)
    {
        RemoveMember(member);
    }

    private void RemoveMember(PartyMemberData member)
    {
        if (member == null)
            return;

        _members.Remove(member);
        NormalizeFormationIndices();
        OnPartyUpdated?.Invoke();
    }

    private void NormalizeFormationIndices()
    {
        _members.Sort((left, right) => left.FormationIndex.CompareTo(right.FormationIndex));

        for (int i = 0; i < _members.Count; i++)
        {
            PartyMemberData member = _members[i];
            if (member == null)
                continue;

            member.FormationIndex = i;
        }
    }
}

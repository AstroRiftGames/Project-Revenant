using Core.Audio;
using Core.Audio.Data;
using UnityEngine;

public readonly struct CorpseInteractionVfxContext
{
    public readonly Vector3 CorpsePosition;
    public readonly Vector3 NecromancerPosition;
    public readonly Sprite CorpseSprite;
    public readonly Vector3 CorpseScale;
    public readonly int SortingLayerId;
    public readonly int SortingOrder;

    public CorpseInteractionVfxContext(Vector3 corpsePosition, Vector3 necromancerPosition, Sprite corpseSprite, Vector3 corpseScale, int sortingLayerId, int sortingOrder)
    {
        CorpsePosition = corpsePosition;
        NecromancerPosition = necromancerPosition;
        CorpseSprite = corpseSprite;
        CorpseScale = corpseScale;
        SortingLayerId = sortingLayerId;
        SortingOrder = sortingOrder;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(RecruitableUnitInteraction))]
[RequireComponent(typeof(UnitDeathHandler))]
[RequireComponent(typeof(PartyMemberLink))]
public class RecruitableCorpseHandler : MonoBehaviour
{
    public static event System.Action<CorpseInteractionVfxContext> AnyCorpseRecruited;
    public static event System.Action<CorpseInteractionVfxContext> AnyCorpseEssenceAbsorbed;

    [SerializeField] private int _minimumRecruitHealth = 1;

    private Unit _unit;
    private RecruitableUnitState _state;
    private UnitDeathHandler _deathHandler;
    private PartyMemberLink _partyLink;
    private NecromancerPartyContext _partyContext;
    private ManaContext _manaContext;
    private SoulContext _soulContext;
    private bool _hasHandledCorpse;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _state = GetComponent<RecruitableUnitState>();
        _deathHandler = GetComponent<UnitDeathHandler>();
        _partyLink = GetComponent<PartyMemberLink>();
        _partyContext = NecromancerPartyContext.Current;
        _manaContext = ManaContext.Current;
        _soulContext = SoulContext.Current;
    }

    private void OnEnable()
    {
        _state.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        _state.OnStateChanged -= HandleStateChanged;
    }

    private CorpseInteractionVfxContext GatherVfxContext()
    {
        Vector3 corpsePos = transform.position;
        Vector3 necromancerPos = corpsePos;
        var necro = NecromancerReferenceUtility.Resolve(null);
        if (necro != null)
        {
            necromancerPos = necro.transform.position;
        }

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Sprite corpseSprite = sr != null ? sr.sprite : null;
        Vector3 scale = sr != null ? sr.transform.lossyScale : transform.lossyScale;
        int sortingLayerId = sr != null ? sr.sortingLayerID : 0;
        int sortingOrder = sr != null ? sr.sortingOrder : 0;

        return new CorpseInteractionVfxContext(corpsePos, necromancerPos, corpseSprite, scale, sortingLayerId, sortingOrder);
    }

    public bool TryRecruit()
    {
        if (!CanHandleCorpse())
            return false;

        UnitData unitData = _unit != null ? _unit.GetUnitData() : null;
        int manaCost = Mathf.Max(0, unitData != null ? unitData.manaCostToRecruit : 0);
        if (!TrySpendMana(manaCost, "recruit"))
            return false;

        if (!TryRecruitIntoParty(out PartyMemberData member))
        {
            RefundMana(manaCost);
            return false;
        }

        AudioService.TryPlayClipFromSet(unitData?.AudioSet, "Recruitment", _unit.transform.position);

        CorpseInteractionVfxContext vfxContext = GatherVfxContext();

        ReviveRecruitedUnit(member);
        _partyContext ??= NecromancerPartyContext.Current;
        _partyContext?.TrackDeployedUnit(gameObject, member.PartyMemberId);
        _hasHandledCorpse = true;

        AnyCorpseRecruited?.Invoke(vfxContext);
        _deathHandler.FinishRecruitment();
        return true;
    }

    public bool TryAbsorbSoul()
    {
        if (!CanHandleCorpse())
            return false;

        _soulContext ??= SoulContext.Current;
        if (_soulContext == null)
            return false;

        UnitData unitData = _unit != null ? _unit.GetUnitData() : null;
        int soulReward = Mathf.Max(0, unitData != null ? unitData.softCurrencyRewardOnSoulAbsorb : 0);
        if (soulReward <= 0)
            return false;

        int manaCost = Mathf.Max(0, unitData != null ? unitData.manaCostToAbsorbSoul : 0);
        if (!TrySpendMana(manaCost, "absorb soul"))
            return false;

        AudioService.TryPlayClipFromSet(unitData?.AudioSet, "Soul Absorption", _unit.transform.position);

        CorpseInteractionVfxContext vfxContext = GatherVfxContext();

        _soulContext.AwardSouls(soulReward);
        _deathHandler.FinishSoulAbsorb();
        _hasHandledCorpse = true;
        AnyCorpseEssenceAbsorbed?.Invoke(vfxContext);
        return true;
    }

    private bool CanHandleCorpse()
    {
        if (_unit != null && _unit.GetComponent<TemporaryCombatUnit>() != null)
            return false;

        return !_hasHandledCorpse && _state.CanInteractWithCorpse;
    }

    private bool TryRecruitIntoParty(out PartyMemberData member)
    {
        member = null;

        _partyContext ??= NecromancerPartyContext.Current;
        return _partyContext != null && _partyContext.TryRecruitUnit(_unit, out member);
    }

    private void ReviveRecruitedUnit(PartyMemberData member)
    {
        if (member == null)
            return;

        _partyLink.Initialize(member.PartyMemberId, true);
        _unit.SetAffiliation(member.RuntimeTeam, member.RuntimeFaction);

        int restoredHealth = Mathf.Clamp(
            member.CurrentHealth,
            Mathf.Max(1, _minimumRecruitHealth),
            _unit.MaxHealth);

        _deathHandler.ReviveUnit(restoredHealth);
    }

    private bool TrySpendMana(int amount, string actionName)
    {
        if (amount <= 0)
            return true;

        _manaContext ??= ManaContext.Current;
        if (_manaContext == null)
        {
            Debug.LogWarning($"[{nameof(RecruitableCorpseHandler)}] Missing {nameof(ManaContext)} while trying to {actionName} on '{name}'.", this);
            return false;
        }

        if (_manaContext.TrySpendMana(amount))
            return true;

        Debug.LogWarning(
            $"[{nameof(RecruitableCorpseHandler)}] Not enough mana to {actionName} on '{name}'. " +
            $"Required: {amount}, current: {(_manaContext.ManaBank != null ? _manaContext.ManaBank.StoredMana : 0)}.",
            this);
        return false;
    }

    private void RefundMana(int amount)
    {
        if (amount <= 0)
            return;

        _manaContext ??= ManaContext.Current;
        _manaContext?.AwardMana(amount);
    }

    private void HandleStateChanged(UnitLifecycleState state)
    {
        _hasHandledCorpse = state != UnitLifecycleState.Recruitable;
    }
}

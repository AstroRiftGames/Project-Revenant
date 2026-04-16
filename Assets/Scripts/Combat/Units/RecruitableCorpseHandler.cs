using System;
using UnityEngine;

public enum RecruitableCorpseResolutionOption
{
    Recruit,
    AbsorbSoul
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(RecruitableUnitInteraction))]
[RequireComponent(typeof(UnitRecruitmentHandler))]
[RequireComponent(typeof(UnitDeathHandler))]
public class RecruitableCorpseHandler : MonoBehaviour, IRoomContextUnitComponent
{
    private Unit _unit;
    private RecruitableUnitState _state;
    private RecruitableUnitInteraction _interaction;
    private UnitRecruitmentHandler _recruitmentHandler;
    private UnitDeathHandler _deathHandler;
    private NecromancerPartyContext _partyContext;
    private SoulContext _soulContext;
    private bool _hasBeenResolved;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _state = GetComponent<RecruitableUnitState>();
        _interaction = GetComponent<RecruitableUnitInteraction>();
        _recruitmentHandler = GetComponent<UnitRecruitmentHandler>();
        _deathHandler = GetComponent<UnitDeathHandler>();
        _partyContext = NecromancerPartyContext.Current;
        _soulContext = SoulContext.Current;
    }

    private void OnEnable()
    {
        if (_interaction != null)
            _interaction.OnInteractionRequested += HandleInteractionRequested;

        if (_state != null)
            _state.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (_interaction != null)
            _interaction.OnInteractionRequested -= HandleInteractionRequested;

        if (_state != null)
            _state.OnStateChanged -= HandleStateChanged;
    }

    public void Configure(NecromancerPartyContext partyContext, SoulContext soulContext)
    {
        _partyContext = partyContext;
        _soulContext = soulContext;
        _recruitmentHandler?.Configure(partyContext);
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        Configure(NecromancerPartyContext.Current, SoulContext.Current);
    }

    public bool TryResolve(RecruitableCorpseResolutionOption option)
    {
        if (!CanResolveRequestedOption(option))
            return false;

        bool resolved = ResolveInteraction(option);

        if (resolved)
            _hasBeenResolved = true;

        return resolved;
    }

    private void HandleInteractionRequested(RecruitableCorpseResolutionOption option)
    {
        TryResolve(option);
    }

    public bool TryRecruit()
    {
        if (!CanResolveRequestedOption(RecruitableCorpseResolutionOption.Recruit))
            return false;

        return _recruitmentHandler != null && _recruitmentHandler.AttemptRecruitment();
    }

    public bool TryAbsorbSoul()
    {
        if (!CanResolveRequestedOption(RecruitableCorpseResolutionOption.AbsorbSoul))
            return false;

        _soulContext ??= SoulContext.Current;
        if (_soulContext == null)
            return false;

        int soulReward = ResolveSoulReward();
        if (soulReward <= 0)
            return false;

        _soulContext.AwardSouls(soulReward);
        _deathHandler.FinalizeSoulAbsorbedCorpse();
        return true;
    }

    private bool CanResolveRequestedOption(RecruitableCorpseResolutionOption option)
    {
        return CanResolveCorpse() && IsSupportedResolution(option);
    }

    private bool CanResolveCorpse()
    {
        return _unit != null &&
               _state != null &&
               _deathHandler != null &&
               !_hasBeenResolved &&
               _state.CanResolveRecruitableCorpse;
    }

    private static bool IsSupportedResolution(RecruitableCorpseResolutionOption option)
    {
        return option == RecruitableCorpseResolutionOption.Recruit ||
               option == RecruitableCorpseResolutionOption.AbsorbSoul;
    }

    private bool ResolveInteraction(RecruitableCorpseResolutionOption option)
    {
        return option switch
        {
            RecruitableCorpseResolutionOption.AbsorbSoul => TryAbsorbSoul(),
            _ => TryRecruit()
        };
    }

    private int ResolveSoulReward()
    {
        UnitData unitData = _unit != null ? _unit.GetUnitData() : null;
        return Mathf.Max(0, unitData != null ? unitData.softCurrencyRewardOnSoulAbsorb : 0);
    }

    private void HandleStateChanged(UnitLifecycleState state)
    {
        _hasBeenResolved = state != UnitLifecycleState.Recruitable;
    }
}

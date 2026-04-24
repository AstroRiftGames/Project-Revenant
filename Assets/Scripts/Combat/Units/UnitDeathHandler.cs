using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
public class UnitDeathHandler : MonoBehaviour
{
    [SerializeField] private Behaviour[] _behavioursToDisableForRecruitableDeath;

    private Unit _unit;
    private LifeController _lifeController;
    private RecruitableUnitState _recruitableState;
    private RecruitableUnitInteraction _recruitableInteraction;
    private bool[] _initialBehaviourEnabledStates = Array.Empty<bool>();
    private bool _hasResolvedDeath;
    private bool _isSoulAbsorbedCorpse;

    public bool HasResolvedDeath => _hasResolvedDeath;
    public bool IsSoulAbsorbedCorpse => _isSoulAbsorbedCorpse;

    public event Action<UnitDeathHandler> OnDeathResolved;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _lifeController = GetComponent<LifeController>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _recruitableInteraction = GetComponent<RecruitableUnitInteraction>();

        if (_behavioursToDisableForRecruitableDeath == null || _behavioursToDisableForRecruitableDeath.Length == 0)
            _behavioursToDisableForRecruitableDeath = ResolveDefaultBehavioursToDisable();

        CacheInitialState();
        ResetDeathState(UnitLifecycleState.Dead);
    }

    public void ResolveDeath()
    {
        if (_hasResolvedDeath)
            return;

        _hasResolvedDeath = true;

        if (_unit != null && _unit.IsEnemy)
        {
            ResolveRecruitableEnemyDeath();
        }
        else
        {
            ResolveDefaultDeath();
        }

        OnDeathResolved?.Invoke(this);
    }

    public void ResetDeathState(UnitLifecycleState state)
    {
        _hasResolvedDeath = false;
        _isSoulAbsorbedCorpse = false;
        GetComponent<UnitMovement>()?.ClearCorpseOccupancy();
        RestoreBehaviours();
        ApplyRecruitableCorpseTransition(state);
    }

    private void ResolveDefaultDeath()
    {
        _isSoulAbsorbedCorpse = false;
        PrepareMovementForDeath();
        GetComponent<UnitMovement>()?.CaptureCorpseOccupancy();
        ApplyRecruitableCorpseTransition(UnitLifecycleState.Dead);
        gameObject.SetActive(false);
    }

    private void ResolveRecruitableEnemyDeath()
    {
        _isSoulAbsorbedCorpse = false;
        EnsureRecruitableComponents();
        PrepareMovementForDeath();
        GetComponent<UnitMovement>()?.CaptureCorpseOccupancy();
        DisableBehavioursForRecruitableDeath();
        EnterRecruitableCorpseState();
    }

    public void FinalizeSoulAbsorbedCorpse()
    {
        _isSoulAbsorbedCorpse = true;
        EnsureRecruitableComponents();
        PrepareMovementForDeath();
        GetComponent<UnitMovement>()?.CaptureCorpseOccupancy();
        DisableBehavioursForRecruitableDeath();
        EnterResolvedCorpseState();
    }

    private void DisableBehavioursForRecruitableDeath()
    {
        if (_behavioursToDisableForRecruitableDeath == null)
            return;

        for (int i = 0; i < _behavioursToDisableForRecruitableDeath.Length; i++)
        {
            Behaviour behaviour = _behavioursToDisableForRecruitableDeath[i];
            if (behaviour == null || ReferenceEquals(behaviour, this))
                continue;

            behaviour.enabled = false;
        }
    }

    private Behaviour[] ResolveDefaultBehavioursToDisable()
    {
        return new Behaviour[]
        {
            GetComponent<UnitBrain>(),
            GetComponent<UnitMovement>(),
            GetComponent<UnitCombat>(),
            GetComponent<SkillCaster>(),
            GetComponent<TargetingStrategy>(),
            GetComponent<UnitAction>(),
            GetComponent<StatusEffectController>(),
            GetComponent<StatusEffectDebugPopupPresenter>()
        };
    }

    private void PrepareMovementForDeath()
    {
        GetComponent<UnitMovement>()?.InterruptMovement();
    }

    private void EnsureRecruitableComponents()
    {
        _recruitableState ??= GetComponent<RecruitableUnitState>() ?? gameObject.AddComponent<RecruitableUnitState>();
        _recruitableInteraction ??= GetComponent<RecruitableUnitInteraction>() ?? gameObject.AddComponent<RecruitableUnitInteraction>();
        UnitRecruitmentHandler recruitmentHandler = GetComponent<UnitRecruitmentHandler>() ?? gameObject.AddComponent<UnitRecruitmentHandler>();
        RecruitableCorpseHandler corpseHandler = GetComponent<RecruitableCorpseHandler>() ?? gameObject.AddComponent<RecruitableCorpseHandler>();
        recruitmentHandler.Configure(NecromancerPartyContext.Current);
        corpseHandler.Configure(NecromancerPartyContext.Current, SoulContext.Current);
    }

    private void EnterRecruitableCorpseState()
    {
        ApplyRecruitableCorpseTransition(UnitLifecycleState.Recruitable);
    }

    private void EnterResolvedCorpseState()
    {
        ApplyRecruitableCorpseTransition(UnitLifecycleState.Dead);
    }

    private void ApplyRecruitableCorpseTransition(UnitLifecycleState state)
    {
        _recruitableState?.SetState(state);
    }

    public void RestoreAliveState()
    {
        ResetDeathState(UnitLifecycleState.Alive);
    }

    public void RestoreOperationalState(int currentHealth)
    {
        if (_lifeController == null)
            return;

        _lifeController.RestoreHealth(currentHealth);
        RestoreAliveState();
    }

    private void CacheInitialState()
    {
        if (_behavioursToDisableForRecruitableDeath != null)
        {
            _initialBehaviourEnabledStates = new bool[_behavioursToDisableForRecruitableDeath.Length];
            for (int i = 0; i < _behavioursToDisableForRecruitableDeath.Length; i++)
            {
                Behaviour behaviour = _behavioursToDisableForRecruitableDeath[i];
                _initialBehaviourEnabledStates[i] = behaviour != null && behaviour.enabled;
            }
        }
    }

    private void RestoreBehaviours()
    {
        if (_behavioursToDisableForRecruitableDeath == null || _initialBehaviourEnabledStates == null)
            return;

        int count = Mathf.Min(_behavioursToDisableForRecruitableDeath.Length, _initialBehaviourEnabledStates.Length);
        for (int i = 0; i < count; i++)
        {
            Behaviour behaviour = _behavioursToDisableForRecruitableDeath[i];
            if (behaviour == null)
                continue;

            behaviour.enabled = _initialBehaviourEnabledStates[i];
        }
    }
}

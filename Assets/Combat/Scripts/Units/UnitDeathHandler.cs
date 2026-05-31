using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
public class UnitDeathHandler : MonoBehaviour
{
    [SerializeField] private Behaviour[] _behavioursToDisableForRecruitableDeath;

    public static event Action<Unit> AnyDeathResolved;

    private Unit _unit;
    private LifeController _lifeController;
    private RecruitableUnitState _recruitableState;
    private RecruitableUnitInteraction _recruitableInteraction;
    private RecruitableCorpseHandler _corpseHandler;
    private UnitMovement _unitMovement;
    private SkillCaster _skillCaster;
    private bool[] _initialBehaviourEnabledStates = Array.Empty<bool>();
    private bool _hasResolvedDeath;
    private bool _isSoulAbsorbedCorpse;

    public bool IsSoulAbsorbedCorpse => _isSoulAbsorbedCorpse;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _lifeController = GetComponent<LifeController>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _recruitableInteraction = GetComponent<RecruitableUnitInteraction>();
        _corpseHandler = GetComponent<RecruitableCorpseHandler>();
        _unitMovement = GetComponent<UnitMovement>();
        _skillCaster = GetComponent<SkillCaster>();

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

        if (_recruitableState == null)
        {
            Debug.LogError($"[{nameof(UnitDeathHandler)}] '{name}' missing RecruitableUnitState. Forcing default death.", this);
            ResolveDefaultDeath();
            NotifyDeathResolved();
            return;
        }

        if (_unit != null && _unit.IsEnemy)
        {
            LeaveRecruitableCorpse();
        }
        else
        {
            ResolveDefaultDeath();
        }

        NotifyDeathResolved();
    }

    public void ResetDeathState(UnitLifecycleState state)
    {
        _hasResolvedDeath = false;
        _isSoulAbsorbedCorpse = false;
        ClearCorpseOccupancy();
        RestoreBehaviours();
        SetLifeState(state);
    }

    private void ResolveDefaultDeath()
    {
        _isSoulAbsorbedCorpse = false;
        PrepareMovementForDeath();
        ClearCorpseOccupancy();
        SetLifeState(UnitLifecycleState.Dead);
        gameObject.SetActive(false);
    }

    private void LeaveRecruitableCorpse()
    {
        _isSoulAbsorbedCorpse = false;

        if (_recruitableInteraction == null || _corpseHandler == null)
        {
            Debug.LogWarning(
                $"[{nameof(UnitDeathHandler)}] '{name}' missing RecruitableUnitInteraction or RecruitableCorpseHandler. " +
                $"Falling back to default death. RecruitableInteraction: {_recruitableInteraction != null}, " +
                $"CorpseHandler: {_corpseHandler != null}.",
                this);
            ResolveDefaultDeath();
            return;
        }

        PrepareMovementForDeath();
        CaptureCorpseOccupancy();
        DisableBehavioursForRecruitableDeath();
        SetLifeState(UnitLifecycleState.Recruitable);
    }

    public void FinishSoulAbsorb()
    {
        _isSoulAbsorbedCorpse = true;
        PrepareMovementForDeath();
        ClearCorpseOccupancy();
        SetLifeState(UnitLifecycleState.Removed);
        gameObject.SetActive(false);
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
            GetComponent<BasicUnitAction>(),
            GetComponent<StatusEffectController>()
        };
    }

    private void PrepareMovementForDeath()
    {
        _skillCaster?.InterruptCast();
        _unitMovement?.InterruptMovement();
    }

    private void SetLifeState(UnitLifecycleState state)
    {
        _recruitableState?.SetState(state);
    }

    private void CaptureCorpseOccupancy()
    {
        _unitMovement?.CaptureCorpseOccupancy();
    }

    private void ClearCorpseOccupancy()
    {
        _unitMovement?.ClearCorpseOccupancy();
    }

    private void NotifyDeathResolved()
    {
        AnyDeathResolved?.Invoke(_unit);
    }

    public void ReviveUnit(int currentHealth)
    {
        if (_lifeController == null)
            return;

        _lifeController.RestoreHealth(currentHealth);
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

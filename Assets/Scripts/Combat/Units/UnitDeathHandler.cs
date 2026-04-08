using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
public class UnitDeathHandler : MonoBehaviour
{
    [Header("Enemy Death Presentation")]
    [SerializeField] private Color _deadEnemyColor = Color.black;
    [SerializeField] private SpriteRenderer[] _spriteRenderers;
    [SerializeField] private Behaviour[] _behavioursToDisableForRecruitableDeath;

    private Unit _unit;
    private RecruitableUnitState _recruitableState;
    private RecruitableUnitInteraction _recruitableInteraction;
    private Color[] _initialSpriteColors = Array.Empty<Color>();
    private bool[] _initialBehaviourEnabledStates = Array.Empty<bool>();
    private bool _hasResolvedDeath;

    public bool HasResolvedDeath => _hasResolvedDeath;

    public event Action<UnitDeathHandler> OnDeathResolved;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _recruitableInteraction = GetComponent<RecruitableUnitInteraction>();

        if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

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
        RestoreSpriteColors();
        RestoreBehaviours();
        _recruitableState?.SetState(state);
        _recruitableInteraction?.SetInteractionEnabled(false);
    }

    private void ResolveDefaultDeath()
    {
        _recruitableState?.SetState(UnitLifecycleState.Dead);
        gameObject.SetActive(false);
    }

    private void ResolveRecruitableEnemyDeath()
    {
        EnsureRecruitableComponents();
        ApplyDeadEnemyVisuals();
        DisableBehavioursForRecruitableDeath();
        _recruitableState?.SetState(UnitLifecycleState.Recruitable);
        _recruitableInteraction?.SetInteractionEnabled(true);
    }

    private void ApplyDeadEnemyVisuals()
    {
        if (_spriteRenderers == null)
            return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            spriteRenderer.color = _deadEnemyColor;
        }
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
            GetComponent<GridInputMover>(),
            GetComponent<TargetingStrategy>(),
            GetComponent<UnitAction>()
        };
    }

    private void EnsureRecruitableComponents()
    {
        _recruitableState ??= GetComponent<RecruitableUnitState>() ?? gameObject.AddComponent<RecruitableUnitState>();
        _recruitableInteraction ??= GetComponent<RecruitableUnitInteraction>() ?? gameObject.AddComponent<RecruitableUnitInteraction>();
        UnitRecruitmentHandler recruitmentHandler = GetComponent<UnitRecruitmentHandler>() ?? gameObject.AddComponent<UnitRecruitmentHandler>();
        recruitmentHandler.Configure(NecromancerPartyContext.Current);
        _recruitableInteraction.SetInteractionEnabled(false);
    }

    public void RestoreAliveState()
    {
        ResetDeathState(UnitLifecycleState.Alive);
    }

    private void CacheInitialState()
    {
        if (_spriteRenderers != null)
        {
            _initialSpriteColors = new Color[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = _spriteRenderers[i];
                _initialSpriteColors[i] = spriteRenderer != null ? spriteRenderer.color : Color.white;
            }
        }

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

    private void RestoreSpriteColors()
    {
        if (_spriteRenderers == null || _initialSpriteColors == null)
            return;

        int count = Mathf.Min(_spriteRenderers.Length, _initialSpriteColors.Length);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            spriteRenderer.color = _initialSpriteColors[i];
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

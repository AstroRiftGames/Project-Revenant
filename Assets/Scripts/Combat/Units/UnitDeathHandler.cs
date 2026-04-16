using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
public class UnitDeathHandler : MonoBehaviour
{
    [Header("Enemy Death Presentation")]
    [SerializeField] private Color _recruitableCorpseColor = Color.black;
    [SerializeField] private Color _soulAbsorbedCorpseColor = new Color(0.239f, 0.239f, 0.239f);
    [SerializeField] private SpriteRenderer[] _spriteRenderers;
    [SerializeField] private Behaviour[] _behavioursToDisableForRecruitableDeath;

    private Unit _unit;
    private LifeController _lifeController;
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
        _lifeController = GetComponent<LifeController>();
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
        GetComponent<UnitMovement>()?.ClearCorpseOccupancy();
        RestoreSpriteColors();
        RestoreBehaviours();
        ApplyRecruitableCorpseTransition(state);
    }

    private void ResolveDefaultDeath()
    {
        GetComponent<UnitMovement>()?.CaptureCorpseOccupancy();
        ApplyRecruitableCorpseTransition(UnitLifecycleState.Dead);
        gameObject.SetActive(false);
    }

    private void ResolveRecruitableEnemyDeath()
    {
        EnsureRecruitableComponents();
        GetComponent<UnitMovement>()?.CaptureCorpseOccupancy();
        ApplyCorpseVisuals(_recruitableCorpseColor);
        DisableBehavioursForRecruitableDeath();
        EnterRecruitableCorpseState();
    }

    public void FinalizeSoulAbsorbedCorpse()
    {
        EnsureRecruitableComponents();
        GetComponent<UnitMovement>()?.CaptureCorpseOccupancy();
        ApplyCorpseVisuals(_soulAbsorbedCorpseColor);
        DisableBehavioursForRecruitableDeath();
        EnterResolvedCorpseState();
    }

    private void ApplyCorpseVisuals(Color color)
    {
        if (_spriteRenderers == null)
            return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            spriteRenderer.color = color;
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
            GetComponent<TargetingStrategy>(),
            GetComponent<UnitAction>()
        };
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

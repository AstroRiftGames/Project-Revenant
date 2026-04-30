using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(RecruitableUnitState))]
public class LifeController : MonoBehaviour, IDamageable
{
    [SerializeField] private bool _debugDamage;

    private Unit _unit;
    private UnitDeathHandler _deathHandler;
    private RecruitableUnitState _recruitableState;
    private StatusEffectController _statusEffectController;
    private UnitMovement _unitMovement;
    private readonly List<Unit> _aggressors = new();
    private bool _hasResolvedDeath;

    public static event Action<Unit> OnUnitDied;
    public static event Action<Unit> OnHealthChanged;
    public Action<int> OnDamageTaken;
    public Action<int> OnLifeUpdated;

    public Unit LastAttacker { get; private set; }

    public int CurrentHealth { get; private set; }
    public int MaxHealth => _unit != null ? _unit.BaseMaxHealth : 0;
    public bool IsAlive => CurrentHealth > 0;
    public UnitLifecycleState LifecycleState => _recruitableState != null
        ? _recruitableState.CurrentState
        : CurrentHealth > 0 ? UnitLifecycleState.Alive : UnitLifecycleState.Dead;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _deathHandler = GetComponent<UnitDeathHandler>() ?? gameObject.AddComponent<UnitDeathHandler>();
        _statusEffectController = GetComponent<StatusEffectController>();
        _unitMovement = GetComponent<UnitMovement>();

        if (_recruitableState == null)
            throw new InvalidOperationException($"[{nameof(LifeController)}] Missing required {nameof(RecruitableUnitState)} on '{name}'.");
    }

    public void Initialize(int maxHealth)
    {
        CurrentHealth = Mathf.Max(0, maxHealth);
        _hasResolvedDeath = false;
        _statusEffectController?.RestoreLivingRuntimeState();
        _deathHandler?.ResetDeathState(UnitLifecycleState.Alive);
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount, IUnit source = null)
    {
        if (!IsAlive || amount <= 0)
            return;

        if (_statusEffectController != null && _statusEffectController.HasInvincibility)
            return;

        if (source is Unit attacker && attacker.IsAlive && attacker != _unit)
        {
            LastAttacker = attacker;
            if (!_aggressors.Contains(attacker))
                _aggressors.Add(attacker);
        }

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        NotifyHealthChanged();
        OnLifeUpdated?.Invoke(CurrentHealth);
        OnDamageTaken?.Invoke(amount);

        if (_statusEffectController != null && _statusEffectController.HasInvisibility)
            _statusEffectController.RemoveEffectOfType(StatusEffectType.Invisibility);

        if (CurrentHealth == 0)
            Die();
    }

    public void Heal(int amount, IUnit source = null)
    {
        if (!IsAlive || amount <= 0 || CurrentHealth >= MaxHealth)
            return;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        NotifyHealthChanged();

        if (_debugDamage && _unit != null)
            Debug.Log($"[Heal] {_unit.Id} healed {amount}. HP: {previousHealth} -> {CurrentHealth}", this);
    }

    public List<Unit> GetAliveAggressors()
    {
        _aggressors.RemoveAll(aggressor => aggressor == null || !aggressor.IsAlive || !aggressor.gameObject.activeInHierarchy);

        if (LastAttacker != null && (!LastAttacker.IsAlive || !LastAttacker.gameObject.activeInHierarchy))
            LastAttacker = null;

        return new List<Unit>(_aggressors);
    }

    private void Die()
    {
        if (_hasResolvedDeath)
            return;

        _hasResolvedDeath = true;

        _unitMovement?.InterruptMovement();
        _statusEffectController?.HandleOwnerDeath();

        if (_debugDamage && _unit != null)
            Debug.Log($"[LifeController] '{_unit.name}' has died by {(LastAttacker != null ? LastAttacker.name : "None")}.", this);
            
        OnUnitDied?.Invoke(_unit);
        if (_deathHandler != null)
        {
            _deathHandler.ResolveDeath();
            return;
        }


        gameObject.SetActive(false);
    }

    public void SetCurrentHealth(int currentHealth)
    {
        CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        NotifyHealthChanged();
    }

    public void Revive(int currentHealth)
    {
        _hasResolvedDeath = false;
        _statusEffectController?.RestoreLivingRuntimeState();
        SetCurrentHealth(Mathf.Max(1, currentHealth));
        OnLifeUpdated?.Invoke(CurrentHealth);
    }

    public void RestoreHealth(int currentHealth)
    {
        Revive(currentHealth);
    }

    private void NotifyHealthChanged()
    {
        if (_unit != null)
            OnHealthChanged?.Invoke(_unit);
    }
}

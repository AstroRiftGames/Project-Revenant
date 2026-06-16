using System;
using UnityEngine;
using System.Collections.Generic;
using Core.Audio;
using Core.Audio.Data;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(UnitDeathHandler))]
public class LifeController : MonoBehaviour, IDamageable
{
    [SerializeField] private bool _debugDamage;

    private Unit _unit;
    private UnitDeathHandler _deathHandler;
    private RecruitableUnitState _recruitableState;
    private StatusEffectController _statusEffectController;
    private ShieldController _shieldController;
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
        _deathHandler = GetComponent<UnitDeathHandler>();
        _statusEffectController = GetComponent<StatusEffectController>();
        _shieldController = GetComponent<ShieldController>();
        _unitMovement = GetComponent<UnitMovement>();

        if (_recruitableState == null || _deathHandler == null)
            throw new InvalidOperationException($"[{nameof(LifeController)}] Missing required death components on '{name}'.");
    }

    private void Start()
    {
        EnsureLivingLifecycleState();
    }

    public void Initialize(int maxHealth)
    {
        CurrentHealth = Mathf.Max(0, maxHealth);
        RestoreLivingRuntimeState();
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount, IUnit source = null)
    {
        if (!CanReceiveCombatLifeEffect() || amount <= 0)
            return;

        if (source is Unit attacker && IsCombatAlive(attacker) && attacker != _unit)
        {
            LastAttacker = attacker;
            if (!_aggressors.Contains(attacker))
                _aggressors.Add(attacker);
        }

        int damageToHealth = amount;
        ShieldController shieldController = ResolveShieldController();
        if (shieldController != null)
        {
            damageToHealth = shieldController.AbsorbDamage(amount, out _);
            if (damageToHealth <= 0)
                return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damageToHealth);
        NotifyHealthChanged();
        OnLifeUpdated?.Invoke(CurrentHealth);
        OnDamageTaken?.Invoke(damageToHealth);
        _unit.GetUnitData().AudioSet.TryGetClip("Damage Taken", out AudioClipConfig clip);
        AudioService.Instance.PlaySFX(clip, transform.position);

        if (CurrentHealth == 0)
            ResolveDeath();
    }

    public void Heal(int amount, IUnit source = null)
    {
        if (!CanReceiveCombatLifeEffect() || amount <= 0 || CurrentHealth >= MaxHealth)
            return;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        NotifyHealthChanged();

        if (_debugDamage && _unit != null)
            Debug.Log($"[Heal] {_unit.Id} healed {amount}. HP: {previousHealth} -> {CurrentHealth}", this);
    }

    public List<Unit> GetAliveAggressors()
    {
        _aggressors.RemoveAll(aggressor => !IsActiveCombatAlive(aggressor));

        if (LastAttacker != null && !IsActiveCombatAlive(LastAttacker))
            LastAttacker = null;

        return new List<Unit>(_aggressors);
    }

    private void ResolveDeath()
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
        if (CurrentHealth > 0 && _recruitableState.CurrentState == UnitLifecycleState.Alive)
            return;

        RestoreLivingRuntimeState();
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

    private void RestoreLivingRuntimeState()
    {
        _hasResolvedDeath = false;
        _statusEffectController?.RestoreLivingRuntimeState();
        _deathHandler?.ResetDeathState(UnitLifecycleState.Alive);
    }

    private void EnsureLivingLifecycleState()
    {
        if (CurrentHealth <= 0 || _deathHandler == null || _recruitableState == null)
            return;

        if (_recruitableState.CurrentState == UnitLifecycleState.Alive)
            return;

        RestoreLivingRuntimeState();
    }

    private bool CanReceiveCombatLifeEffect()
    {
        return IsAlive && LifecycleState == UnitLifecycleState.Alive;
    }

    private ShieldController ResolveShieldController()
    {
        if (_shieldController != null)
            return _shieldController;

        _shieldController = GetComponent<ShieldController>();
        return _shieldController;
    }

    private static bool IsCombatAlive(Unit unit)
    {
        return unit != null &&
               unit.IsAlive &&
               unit.LifecycleState == UnitLifecycleState.Alive;
    }

    private static bool IsActiveCombatAlive(Unit unit)
    {
        return IsCombatAlive(unit) && unit.gameObject.activeInHierarchy;
    }
}

using UnityEngine;
using System.Collections.Generic;
using System;

public class LifeController : MonoBehaviour, IDamageable
{
    [SerializeField] private bool _debugDamage;

    private Unit _unit;
    private readonly List<Unit> _aggressors = new();

    public static event Action<Unit> OnUnitDied;
    public static event Action<Unit> OnHealthChanged;

    public Unit LastAttacker { get; private set; }

    public int CurrentHealth { get; private set; }
    public int MaxHealth => _unit != null ? _unit.BaseMaxHealth : 0;
    public bool IsAlive => CurrentHealth > 0;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    public void Initialize(int maxHealth)
    {
        CurrentHealth = Mathf.Max(0, maxHealth);
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount, IUnit source = null)
    {
        if (!IsAlive || amount <= 0)
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

        if (_debugDamage && _unit != null)
            Debug.Log($"[Damage] {_unit.Id} took {amount}. HP: {previousHealth} -> {CurrentHealth}", this);

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
        if (_debugDamage && _unit != null)
            Debug.Log($"[Death] {_unit.Id} died.", this);

        OnUnitDied?.Invoke(_unit);
        gameObject.SetActive(false);
    }

    public void SetCurrentHealth(int currentHealth)
    {
        CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        if (_unit != null)
            OnHealthChanged?.Invoke(_unit);
    }
}

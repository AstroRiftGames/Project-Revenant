using UnityEngine;
using System.Collections.Generic;

public class LifeController : MonoBehaviour, IDamageable
{
    [SerializeField] private bool _debugDamage;

    private Unit _unit;
    private readonly List<Unit> _aggressors = new();

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

        if (_debugDamage && _unit != null)
            Debug.Log($"[Damage] {_unit.Id} took {amount}. HP: {previousHealth} -> {CurrentHealth}", this);

        if (CurrentHealth == 0)
            Die();
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

        gameObject.SetActive(false);
    }
}

using UnityEngine;

[RequireComponent(typeof(Unit))]
public class LifeController : MonoBehaviour, IDamageable
{
    [SerializeField] private bool _debugDamage;

    private Unit _unit;

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

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (_debugDamage && _unit != null)
            Debug.Log($"[Damage] {_unit.Id} took {amount}. HP: {previousHealth} -> {CurrentHealth}", this);

        if (CurrentHealth == 0)
            Die();
    }

    private void Die()
    {
        if (_debugDamage && _unit != null)
            Debug.Log($"[Death] {_unit.Id} died.", this);

        gameObject.SetActive(false);
    }
}

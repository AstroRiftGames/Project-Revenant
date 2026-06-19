public enum DamageSourceKind
{
    Direct,
    DoT,
    Debug
}

public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    void TakeDamage(int amount, IUnit source = null, DamageSourceKind sourceKind = DamageSourceKind.Direct);
}

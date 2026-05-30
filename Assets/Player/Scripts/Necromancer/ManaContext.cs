using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ManaBank))]
public class ManaContext : MonoBehaviour
{
    public static ManaContext Current { get; private set; }

    [SerializeField] private ManaBank _manaBank;

    public ManaBank ManaBank => _manaBank;

    private void OnEnable()
    {
        Current = this;
    }

    private void OnDisable()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
    }

    public void Configure(ManaBank manaBank)
    {
        _manaBank = manaBank;
    }

    public int AwardMana(int amount)
    {
        if (_manaBank == null || amount <= 0)
            return 0;

        return _manaBank.Deposit(amount);
    }

    public bool TrySpendMana(int amount)
    {
        if (amount <= 0)
            return true;

        return _manaBank != null && _manaBank.Withdraw(amount);
    }

    public bool HasEnoughMana(int amount)
    {
        return amount <= 0 || (_manaBank != null && _manaBank.HasEnough(amount));
    }
}

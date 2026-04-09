using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SoulContext : MonoBehaviour
{
    public static SoulContext Current { get; private set; }

    [SerializeField] private SoulBank _soulBank;

    public SoulBank SoulBank => _soulBank;

    private void OnEnable()
    {
        Current = this;
    }

    private void OnDisable()
    {
        if (ReferenceEquals(Current, this))
            Current = null;
    }

    public void Configure(SoulBank soulBank)
    {
        _soulBank = soulBank;
    }

    public int AwardSouls(int amount)
    {
        if (_soulBank == null || amount <= 0)
            return 0;

        return _soulBank.Deposit(amount);
    }
}

[DisallowMultipleComponent]
public class SoulBank : MonoBehaviour
{
    [SerializeField] private int _storedSouls;

    public int StoredSouls => _storedSouls;

    public event Action<int> OnSoulsChanged;

    public int Deposit(int amount)
    {
        if (amount <= 0)
            return _storedSouls;

        _storedSouls += amount;
        OnSoulsChanged?.Invoke(_storedSouls);
        return _storedSouls;
    }
}

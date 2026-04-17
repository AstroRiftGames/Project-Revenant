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

    /// <summary>Fires with (newTotal, delta). Delta is positive for gains, negative for spending.</summary>
    public event Action<int, int> OnSoulsChanged;

    public int Deposit(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[SoulBank] Deposit ignored — amount ({amount}) must be > 0.");
            return _storedSouls;
        }

        _storedSouls += amount;
        Debug.Log($"[SoulBank] Deposit +{amount} — total: {_storedSouls} — listeners on OnSoulsChanged: {OnSoulsChanged?.GetInvocationList().Length ?? 0}");
        OnSoulsChanged?.Invoke(_storedSouls, amount);
        return _storedSouls;
    }

    /// <summary>Spends souls. Returns true if there were enough funds.</summary>
    public bool Withdraw(int amount)
    {
        if (amount <= 0 || amount > _storedSouls)
        {
            Debug.LogWarning($"[SoulBank] Withdraw failed — amount: {amount}, stored: {_storedSouls}. Needs amount > 0 and enough funds.");
            return false;
        }

        _storedSouls -= amount;
        Debug.Log($"[SoulBank] Withdraw -{amount} — total: {_storedSouls} — listeners on OnSoulsChanged: {OnSoulsChanged?.GetInvocationList().Length ?? 0}");
        OnSoulsChanged?.Invoke(_storedSouls, -amount);
        return true;
    }

#if UNITY_EDITOR
    [SerializeField] private int _testAmount = 10;

    [ContextMenu("Test — Deposit Souls")]
    private void TestDeposit() => Deposit(_testAmount);

    [ContextMenu("Test — Withdraw Souls")]
    private void TestWithdraw() => Withdraw(_testAmount);
#endif
}

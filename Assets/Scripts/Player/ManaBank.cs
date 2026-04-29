using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ManaBank : MonoBehaviour
{
    [SerializeField] private int _storedMana = 10;

    public int StoredMana => _storedMana;

    public event Action<int, int> OnManaChanged;

    public bool HasEnough(int amount)
    {
        return amount <= 0 || _storedMana >= amount;
    }

    public int Deposit(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[{nameof(ManaBank)}] Deposit ignored because amount ({amount}) must be > 0.");
            return _storedMana;
        }

        _storedMana += amount;
        OnManaChanged?.Invoke(_storedMana, amount);
        return _storedMana;
    }

    public bool Withdraw(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[{nameof(ManaBank)}] Withdraw ignored because amount ({amount}) must be > 0.");
            return false;
        }

        if (_storedMana < amount)
        {
            Debug.LogWarning($"[{nameof(ManaBank)}] Withdraw failed. Requested: {amount}, stored: {_storedMana}.");
            return false;
        }

        _storedMana -= amount;
        OnManaChanged?.Invoke(_storedMana, -amount);
        return true;
    }

#if UNITY_EDITOR
    [SerializeField] private int _testAmount = 1;

    [ContextMenu("Test - Deposit Mana")]
    private void TestDeposit() => Deposit(_testAmount);

    [ContextMenu("Test - Withdraw Mana")]
    private void TestWithdraw() => Withdraw(_testAmount);
#endif
}

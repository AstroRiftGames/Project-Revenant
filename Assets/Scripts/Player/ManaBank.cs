using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ManaBank : MonoBehaviour
{
    [SerializeField] private int _maximumMana = 10;
    [SerializeField] private int _storedMana = 10;

    public int StoredMana => _storedMana;
    public int MaximumMana => _maximumMana;

    public event Action<int, int> OnManaChanged;
    public event Action<int, int> OnMaximumManaChanged;

    private void Awake()
    {
        _maximumMana = Mathf.Max(0, _maximumMana);
        _storedMana = Mathf.Clamp(_storedMana, 0, _maximumMana);
    }

    public void Initialize(int maximumMana, bool fillToMax = true)
    {
        _maximumMana = Mathf.Max(0, maximumMana);
        _storedMana = fillToMax ? _maximumMana : Mathf.Clamp(_storedMana, 0, _maximumMana);
        OnMaximumManaChanged?.Invoke(_maximumMana, 0);
        OnManaChanged?.Invoke(_storedMana, 0);
    }

    public bool HasEnough(int amount)
    {
        return amount <= 0 || _storedMana >= amount;
    }

    public void SetMaximumMana(int maximumMana)
    {
        int nextMaximumMana = Mathf.Max(0, maximumMana);
        if (_maximumMana == nextMaximumMana)
            return;

        int previousMaximumMana = _maximumMana;
        int previousStoredMana = _storedMana;
        int capacityDelta = nextMaximumMana - previousMaximumMana;

        _maximumMana = nextMaximumMana;
        if (capacityDelta > 0)
        {
            _storedMana = Mathf.Min(_storedMana + capacityDelta, _maximumMana);
        }
        else
        {
            _storedMana = Mathf.Clamp(_storedMana, 0, _maximumMana);
        }

        OnMaximumManaChanged?.Invoke(_maximumMana, capacityDelta);

        int manaDelta = _storedMana - previousStoredMana;
        if (manaDelta != 0)
            OnManaChanged?.Invoke(_storedMana, manaDelta);
    }

    public int Deposit(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[{nameof(ManaBank)}] Deposit ignored because amount ({amount}) must be > 0.");
            return _storedMana;
        }

        int appliedAmount = Mathf.Min(amount, Mathf.Max(0, _maximumMana - _storedMana));
        if (appliedAmount <= 0)
            return _storedMana;

        _storedMana += appliedAmount;
        OnManaChanged?.Invoke(_storedMana, appliedAmount);
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

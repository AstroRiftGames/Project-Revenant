using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShieldController : MonoBehaviour
{
    private int _currentShield;
    private float _expiresAt;

    public event Action<int> OnShieldChanged;
    public event Action<int> OnShieldAbsorbed;
    public static event Action<ShieldController, int, int> AnyShieldChanged;

    public int CurrentShield => _currentShield;
    public bool HasShield => _currentShield > 0;

    private void Update()
    {
        if (!HasShield)
            return;

        if (Time.time < _expiresAt)
            return;

        ClearShield();
    }

    private void OnDisable()
    {
        ClearShield();
    }

    public void ApplyShield(int amount, float durationSeconds)
    {
        if (amount <= 0 || durationSeconds <= 0f)
            return;

        int prevShield = _currentShield;
        _currentShield = amount;
        _expiresAt = Time.time + durationSeconds;
        OnShieldChanged?.Invoke(_currentShield);
        AnyShieldChanged?.Invoke(this, prevShield, _currentShield);
    }

    public int AbsorbDamage(int incomingDamage, out int absorbedDamage)
    {
        absorbedDamage = 0;
        if (incomingDamage <= 0)
            return 0;

        if (!HasShield)
            return incomingDamage;

        int prevShield = _currentShield;
        absorbedDamage = Mathf.Min(_currentShield, incomingDamage);
        _currentShield = Mathf.Max(0, _currentShield - absorbedDamage);
        OnShieldAbsorbed?.Invoke(absorbedDamage);
        OnShieldChanged?.Invoke(_currentShield);
        AnyShieldChanged?.Invoke(this, prevShield, _currentShield);

        if (_currentShield <= 0)
            _expiresAt = 0f;

        return Mathf.Max(0, incomingDamage - absorbedDamage);
    }

    private void ClearShield()
    {
        if (_currentShield <= 0 && _expiresAt <= 0f)
            return;

        int prevShield = _currentShield;
        _currentShield = 0;
        _expiresAt = 0f;
        OnShieldChanged?.Invoke(_currentShield);
        AnyShieldChanged?.Invoke(this, prevShield, _currentShield);
    }
}

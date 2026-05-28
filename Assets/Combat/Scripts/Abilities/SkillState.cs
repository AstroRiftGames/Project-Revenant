using UnityEngine;

public sealed class SkillState
{
    public bool IsChargeReady => CurrentCharge >= MaxCharge;
    public float CurrentCharge { get; private set; }
    public float MaxCharge { get; private set; } = 100f;

    public void ConfigureCharge(float maxCharge, float initialCharge = 0f)
    {
        MaxCharge = Mathf.Max(1f, maxCharge);
        CurrentCharge = Mathf.Clamp(initialCharge, 0f, MaxCharge);
    }

    public void AddCharge(float amount)
    {
        if (amount <= 0f)
            return;

        CurrentCharge = Mathf.Clamp(CurrentCharge + amount, 0f, MaxCharge);
    }

    public void ResetCharge()
    {
        CurrentCharge = 0f;
    }

    public void Reset()
    {
        ResetCharge();
    }
}

using UnityEngine;

public sealed class SkillState
{
    public bool IsReady => RemainingCooldown <= 0f;
    public float RemainingCooldown { get; private set; }

    public void StartCooldown(float duration)
    {
        RemainingCooldown = Mathf.Max(0f, duration);
    }

    public void Tick(float deltaTime)
    {
        if (RemainingCooldown <= 0f)
            return;

        RemainingCooldown = Mathf.Max(0f, RemainingCooldown - Mathf.Max(0f, deltaTime));
    }

    public void Reset()
    {
        RemainingCooldown = 0f;
    }
}

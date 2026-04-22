using UnityEngine;

public sealed class SkillState
{
    public float NextReadyTime { get; private set; }

    public bool IsReady => Time.time >= NextReadyTime;
    public float RemainingCooldown => Mathf.Max(0f, NextReadyTime - Time.time);

    public void StartCooldown(float duration)
    {
        NextReadyTime = Time.time + Mathf.Max(0f, duration);
    }

    public void Reset()
    {
        NextReadyTime = 0f;
    }
}

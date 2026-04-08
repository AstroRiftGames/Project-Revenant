using System;
using UnityEngine;

[DisallowMultipleComponent]
public class RecruitableUnitState : MonoBehaviour
{
    public UnitLifecycleState CurrentState { get; private set; } = UnitLifecycleState.Dead;
    public bool IsAlive => CurrentState == UnitLifecycleState.Alive;
    public bool IsRecruitable => CurrentState == UnitLifecycleState.Recruitable;
    public bool IsDead => CurrentState == UnitLifecycleState.Dead;

    public event Action<UnitLifecycleState> OnStateChanged;
    public event Action<bool> OnRecruitableStateChanged;

    public void SetState(UnitLifecycleState state)
    {
        if (CurrentState == state)
            return;

        CurrentState = state;
        OnStateChanged?.Invoke(CurrentState);
        OnRecruitableStateChanged?.Invoke(IsRecruitable);
    }
}

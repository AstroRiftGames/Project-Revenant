using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RecruitableUnitState))]
public class RecruitableUnitInteraction : MonoBehaviour, IInteractable
{
    private RecruitableUnitState _recruitableState;
    private bool _interactionEnabled;

    public bool IsInteractionEnabled => _interactionEnabled &&
                                        _recruitableState != null &&
                                        _recruitableState.CurrentState == UnitLifecycleState.Recruitable;

    public event Action<RecruitableUnitInteraction> OnInteractionRequested;

    private void Awake()
    {
        _recruitableState = GetComponent<RecruitableUnitState>();
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        _interactionEnabled = isEnabled;
    }

    public void Interact()
    {
        if (!IsInteractionEnabled)
            return;

        OnInteractionRequested?.Invoke(this);
    }
}

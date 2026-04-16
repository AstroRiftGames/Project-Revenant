using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RecruitableUnitState))]
public class RecruitableUnitInteraction : MonoBehaviour, IInteractable
{
    private RecruitableUnitState _recruitableState;
    private bool _interactionEnabled;

    public bool IsInteractionAvailable => _interactionEnabled &&
                                          _recruitableState != null &&
                                          _recruitableState.CanResolveRecruitableCorpse;
    public bool IsInteractionEnabled => IsInteractionAvailable;

    public event Action<bool> OnInteractionAvailabilityChanged;
    public event Action<RecruitableCorpseResolutionOption> OnInteractionRequested;

    private void Awake()
    {
        _recruitableState = GetComponent<RecruitableUnitState>();
    }

    private void OnEnable()
    {
        if (_recruitableState != null)
            _recruitableState.OnStateChanged += HandleStateChanged;

        NotifyInteractionAvailabilityChanged(forceEvent: true);
    }

    private void OnDisable()
    {
        if (_recruitableState != null)
            _recruitableState.OnStateChanged -= HandleStateChanged;

        NotifyInteractionAvailabilityChanged(forceEvent: true, overrideAvailability: false);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        bool previousAvailability = IsInteractionAvailable;
        _interactionEnabled = isEnabled;
        NotifyInteractionAvailabilityChanged(previousAvailability);
    }

    public void Interact()
    {
        if (!IsInteractionAvailable)
            return;

        RequestInteractionResolution(ResolveRequestedOption());
    }

    private void HandleStateChanged(UnitLifecycleState _)
    {
        NotifyInteractionAvailabilityChanged(forceEvent: false);
    }

    private void NotifyInteractionAvailabilityChanged(bool previousAvailability)
    {
        bool currentAvailability = IsInteractionAvailable;
        if (previousAvailability == currentAvailability)
            return;

        OnInteractionAvailabilityChanged?.Invoke(currentAvailability);
    }

    private void NotifyInteractionAvailabilityChanged(bool forceEvent, bool? overrideAvailability = null)
    {
        if (!forceEvent)
            return;

        OnInteractionAvailabilityChanged?.Invoke(overrideAvailability ?? IsInteractionAvailable);
    }

    private RecruitableCorpseResolutionOption ResolveRequestedOption()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? RecruitableCorpseResolutionOption.AbsorbSoul
            : RecruitableCorpseResolutionOption.Recruit;
    }

    private void RequestInteractionResolution(RecruitableCorpseResolutionOption option)
    {
        OnInteractionRequested?.Invoke(option);
    }
}

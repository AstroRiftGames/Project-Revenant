using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(RecruitableCorpseHandler))]
public class RecruitableUnitInteraction : MonoBehaviour, IInteractable
{
    private RecruitableUnitState _recruitableState;
    private RecruitableCorpseHandler _corpseHandler;

    public bool IsInteractionAvailable => _recruitableState.CanInteractWithCorpse;

    public event System.Action<bool> OnInteractionAvailabilityChanged;

    private void Awake()
    {
        _recruitableState = GetComponent<RecruitableUnitState>();
        _corpseHandler = GetComponent<RecruitableCorpseHandler>();
    }

    private void OnEnable()
    {
        _recruitableState.OnStateChanged += HandleStateChanged;

        NotifyInteractionAvailabilityChanged(forceEvent: true);
    }

    private void OnDisable()
    {
        _recruitableState.OnStateChanged -= HandleStateChanged;

        NotifyInteractionAvailabilityChanged(forceEvent: true, overrideAvailability: false);
    }

    public void Interact()
    {
        if (!IsInteractionAvailable)
            return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            _corpseHandler?.TryAbsorbSoul();
        else
            _corpseHandler?.TryRecruit();
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

}

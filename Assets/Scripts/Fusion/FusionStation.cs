using System;
using UnityEngine;

public class FusionStation : MonoBehaviour, IInteractable
{
    public event Action OnInteraction;
    public event Action<bool> OnInteractionAvailabilityChanged;

    public bool IsInteractionAvailable => isActiveAndEnabled;

    private void OnEnable()
    {
        OnInteractionAvailabilityChanged?.Invoke(IsInteractionAvailable);
    }

    private void OnDisable()
    {
        OnInteractionAvailabilityChanged?.Invoke(false);
    }

    public void Interact()
    {
        if (!IsInteractionAvailable)
            return;

        OnInteraction?.Invoke();
    }
}


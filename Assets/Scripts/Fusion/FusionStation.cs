using System;
using UnityEngine;

public class FusionStation : MonoBehaviour, IInteractable
{
    public event Action OnInteraction;

    public void Interact()
    {
        OnInteraction?.Invoke();
    }
}


using System;
using UnityEngine;

public class RoomDoor : MonoBehaviour, IInteractable
{
    [Header("Connected Rooms")]
    public GameObject roomA;
    public GameObject roomB;

    public static event Action<RoomDoor> OnDoorInteracted;

    [ContextMenu("Interact")]
    public void Interact()
    {
        Debug.Log("Interacted with the door!");
        OnDoorInteracted?.Invoke(this);
    }

    public void HandleTriggerEnter(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Interact();
    }
}
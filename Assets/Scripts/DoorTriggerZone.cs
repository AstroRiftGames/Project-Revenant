using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorTriggerZone : MonoBehaviour
{
    private RoomDoor _door;

    private void Awake()
    {
        _door = GetComponentInParent<RoomDoor>();

        if (_door == null)
            Debug.LogError($"[DoorTriggerZone] No se encontró RoomDoor en {transform.parent?.name ?? gameObject.name}", this);

#if UNITY_EDITOR
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[DoorTriggerZone] El Collider2D en {gameObject.name} no está en modo Trigger.", this);
#endif
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_door == null)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (other.GetComponent<NecromancerDoorDetector>() != null)
            return;

        _door.Interact();
    }
}

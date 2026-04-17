using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private bool _allowTriggerInteraction;
    [SerializeField] private RoomDoor _roomDoor;

    private void Awake()
    {
        if(_roomDoor == null)
        {
            _roomDoor = GetComponentInParent<RoomDoor>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_allowTriggerInteraction)
            return;

        _roomDoor?.TryInteractFromTrigger(other);
    }
}

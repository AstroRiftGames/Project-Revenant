using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
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
        _roomDoor?.TryInteractFromTrigger(other);
    }
}

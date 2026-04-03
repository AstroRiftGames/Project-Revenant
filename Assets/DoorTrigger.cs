using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    private RoomDoor _roomDoor;

    private void Awake()
    {
        _roomDoor = GetComponentInParent<RoomDoor>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        _roomDoor?.HandleTriggerEnter(other);
    }
}

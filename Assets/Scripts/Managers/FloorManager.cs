using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [Header("Estado")]
    [SerializeField] private GameObject _currentRoom;

    public GameObject CurrentRoom => _currentRoom;

    private void OnEnable()
    {
        RoomDoor.OnDoorInteracted += HandleRoomTransition;
    }

    private void OnDisable()
    {
        RoomDoor.OnDoorInteracted -= HandleRoomTransition;
    }

    public void EnterRoom(GameObject nextRoom)
    {
        if (nextRoom == null)
        {
            Debug.LogWarning("[FloorManager] EnterRoom: nextRoom es null.", this);
            return;
        }

        if (nextRoom == _currentRoom)
            return;

        GameObject previousRoom = _currentRoom;

        nextRoom.SetActive(true);

        _currentRoom = nextRoom;

        if (nextRoom.TryGetComponent(out RoomContext roomContext))
            roomContext.InitializeRoom();
        else
            Debug.LogWarning($"[FloorManager] La sala '{nextRoom.name}' no tiene RoomContext.", this);

        if (previousRoom != null)
            previousRoom.SetActive(false);
    }

    private void HandleRoomTransition(RoomDoor door)
    {
        if (_currentRoom == null)
        {
            Debug.LogWarning("[FloorManager] HandleRoomTransition: no hay sala actual asignada.", this);
            return;
        }

        if (_currentRoom == door.roomA)
            EnterRoom(door.roomB);
        else if (_currentRoom == door.roomB)
            EnterRoom(door.roomA);
        else
            Debug.LogWarning($"[FloorManager] La sala actual '{_currentRoom.name}' no está conectada a esta puerta.", this);
    }
}

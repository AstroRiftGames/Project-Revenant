using System;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private GameObject _currentRoom;

    public GameObject CurrentRoom => _currentRoom;

    public static event Action<RoomDoor, GameObject> OnRoomEntered;

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
        Debug.Log($"[FloorManager] currentRoom={_currentRoom?.name ?? "null"} doorA={door.roomA?.name ?? "null"} doorB={door.roomB?.name ?? "null"}", this);

        if (_currentRoom == null)
        {
            Debug.LogWarning("[FloorManager] HandleRoomTransition: no hay sala actual asignada.", this);
            return;
        }

        GameObject nextRoom;
        if (_currentRoom == door.roomA)
            nextRoom = door.roomB;
        else if (_currentRoom == door.roomB)
            nextRoom = door.roomA;
        else
        {
            Debug.LogWarning($"[FloorManager] La sala actual '{_currentRoom.name}' no está conectada a esta puerta.", this);
            return;
        }

        EnterRoom(nextRoom);
        OnRoomEntered?.Invoke(door, nextRoom);
    }
}

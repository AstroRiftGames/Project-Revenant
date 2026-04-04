using System;
using UnityEngine;
using PrefabDungeonGeneration;

public class FloorManager : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private GameObject _currentRoom;

    public GameObject CurrentRoom => _currentRoom;

    public static event Action<RoomDoor, GameObject> OnRoomEntered;
    public static event Action OnNextFloorRequested;

    [ContextMenu("Request Next Floor")]
    public void RequestNextFloor()
    {
        OnNextFloorRequested?.Invoke();
    }

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
            return;
        }

        if (nextRoom == _currentRoom)
            return;

        GameObject previousRoom = _currentRoom;

        nextRoom.SetActive(true);

        _currentRoom = nextRoom;

        if (nextRoom.TryGetComponent(out RoomContext roomContext))
            roomContext.InitializeRoom();

        TriggerRoomContentGeneration(nextRoom);

        if (previousRoom != null)
            previousRoom.SetActive(false);
    }

    private void HandleRoomTransition(RoomDoor door)
    {
        if (_currentRoom == null)
        {
            return;
        }

        GameObject nextRoom;
        if (_currentRoom == door.roomA)
            nextRoom = door.roomB;
        else if (_currentRoom == door.roomB)
            nextRoom = door.roomA;
        else
        {
            return;
        }

        EnterRoom(nextRoom);
        OnRoomEntered?.Invoke(door, nextRoom);
    }

    private void TriggerRoomContentGeneration(GameObject room)
    {
        if (room == null) return;

        RoomContentGenerator generator = room.GetComponentInChildren<RoomContentGenerator>();
        if (generator != null)
        {
            generator.GenerateContent();
        }
    }
}

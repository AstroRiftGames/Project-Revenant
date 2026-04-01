using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [Header("State")]
    public GameObject currentRoom;

    private void OnEnable()
    {
        RoomDoor.OnDoorInteracted += HandleRoomTransition;
    }

    private void OnDisable()
    {
        RoomDoor.OnDoorInteracted -= HandleRoomTransition;
    }

    private void HandleRoomTransition(RoomDoor door)
    {
        if (currentRoom == null)
        {
            Debug.LogWarning("RoomManager: currentRoom no está asignada.");
            return;
        }

        GameObject nextRoom = null;

        if (currentRoom == door.roomA)
        {
            nextRoom = door.roomB;
        }
        else if (currentRoom == door.roomB)
        {
            nextRoom = door.roomA;
        }
        else
        {
            Debug.LogWarning("RoomManager: La currentRoom no coincide con ninguna sala conectada a la puerta.");
            return;
        }

        if (nextRoom != null)
        {
            // Activa el contenido de la nueva sala
            nextRoom.SetActive(true);

            // Desactiva el contenido de la sala vieja
            currentRoom.SetActive(false);

            // Actualiza la referencia a la nueva sala
            currentRoom = nextRoom;
        }
    }
}

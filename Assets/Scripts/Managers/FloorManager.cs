using UnityEngine;
using UnityEngine.Tilemaps;
using PrefabDungeonGeneration;

public class FloorManager : MonoBehaviour
{
    [Header("State")]
    public GameObject currentRoom;
    [Header("References")]
    [SerializeField] private NecromancerMover _necromancerPrefab;
    [SerializeField] private RoomFramingCamera _roomFramingCamera;

    private NecromancerMover _necromancerInstance;

    public void SetInitialRoom(GameObject initialRoom)
    {
        if (initialRoom == null)
        {
            Debug.LogWarning("RoomManager: initialRoom no esta asignada.");
            return;
        }

        currentRoom = initialRoom;
        TriggerRoomContentGeneration(initialRoom);
        MovePlayerToRoom(initialRoom);
        NotifyCameraRoomSnap(initialRoom);
    }

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
            Debug.LogWarning("RoomManager: currentRoom no esta asignada.");
            return;
        }

        GameObject previousRoom = currentRoom;
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

        if (nextRoom == null)
            return;

        nextRoom.SetActive(true);
        TriggerRoomContentGeneration(nextRoom);
        previousRoom.SetActive(false);
        currentRoom = nextRoom;

        MovePlayerToRoom(nextRoom);
        NotifyCameraRoomChanged(nextRoom);
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

    private void MovePlayerToRoom(GameObject room)
    {
        if (room == null)
            return;

        if (!TryResolveNecromancerMover(out NecromancerMover mover))
            return;

        Tilemap[] tilemaps = room.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null || tilemap.gameObject.name != "FloorTilemap")
                continue;

            Vector3Int centerCell = GetCenterFloorCell(tilemap);
            Vector3 spawnPosition = tilemap.GetCellCenterWorld(centerCell);
            spawnPosition.z = mover.transform.position.z;
            mover.Teleport(spawnPosition);
            return;
        }
    }

    private bool TryResolveNecromancerMover(out NecromancerMover mover)
    {
        if (_necromancerInstance != null)
        {
            mover = _necromancerInstance;
            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _necromancerInstance = player.GetComponent<NecromancerMover>();
            mover = _necromancerInstance;
            return mover != null;
        }

        if (_necromancerPrefab == null)
        {
            Debug.LogWarning("RoomManager: No hay instancia del Necromancer en escena ni prefab asignado.");
            mover = null;
            return false;
        }

        _necromancerInstance = Instantiate(_necromancerPrefab);
        mover = _necromancerInstance;
        return true;
    }

    private Vector3Int GetCenterFloorCell(Tilemap tilemap)
    {
        tilemap.CompressBounds();

        BoundsInt bounds = tilemap.cellBounds;
        Vector2 boundsCenter = new Vector2(
            (bounds.xMin + bounds.xMax - 1) * 0.5f,
            (bounds.yMin + bounds.yMax - 1) * 0.5f);

        Vector3Int bestCell = Vector3Int.zero;
        float bestDistance = float.MaxValue;
        bool foundCell = false;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell))
                    continue;

                float distance = Vector2.SqrMagnitude(new Vector2(cell.x, cell.y) - boundsCenter);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestCell = cell;
                foundCell = true;
            }
        }

        return foundCell ? bestCell : bounds.position;
    }

    private void NotifyCameraRoomSnap(GameObject room)
    {
        RoomFramingCamera cameraController = ResolveRoomFramingCamera();
        if (cameraController == null)
            return;

        cameraController.SnapToRoom(room);
    }

    private void NotifyCameraRoomChanged(GameObject room)
    {
        RoomFramingCamera cameraController = ResolveRoomFramingCamera();
        if (cameraController == null)
            return;

        cameraController.OnRoomChanged(room);
    }

    private RoomFramingCamera ResolveRoomFramingCamera()
    {
        if (_roomFramingCamera != null)
            return _roomFramingCamera;

        _roomFramingCamera = FindFirstObjectByType<RoomFramingCamera>();
        return _roomFramingCamera;
    }
}

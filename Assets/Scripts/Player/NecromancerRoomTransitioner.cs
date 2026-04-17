using UnityEngine;

public class NecromancerRoomTransitioner : MonoBehaviour
{
    private static readonly Vector3Int[] CardinalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    private Necromancer _necromancer;

    private void OnEnable()
    {
        FloorManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        FloorManager.OnRoomEntered -= HandleRoomEntered;
    }

    private void HandleRoomEntered(RoomDoor door, GameObject newRoom)
    {
        MoveNecromancerToRoom(door, newRoom);
    }

    public bool MoveNecromancerToRoom(GameObject roomObject)
    {
        return MoveNecromancerToRoom(null, roomObject);
    }

    public bool MoveNecromancerToRoom(RoomDoor enteredDoor, GameObject roomObject)
    {
        if (_necromancer == null)
            _necromancer = FindAnyObjectByType<Necromancer>();

        if (_necromancer == null)
        {
            Debug.LogWarning("[NecromancerRoomTransitioner] No necromancer was found in the scene.", this);
            return false;
        }

        if (roomObject == null || !roomObject.TryGetComponent(out RoomContext roomContext))
        {
            Debug.LogWarning($"[NecromancerRoomTransitioner] '{roomObject?.name ?? "NULL"}' does not have a RoomContext.", this);
            return false;
        }

        RoomGrid grid = roomContext.RoomGrid;
        if (grid == null)
        {
            Debug.LogWarning($"[NecromancerRoomTransitioner] RoomContext for '{roomObject.name}' does not have a BattleGrid.", this);
            return false;
        }

        _necromancer.SetGrid(grid);

        Vector3Int spawnCell = ResolveArrivalCell(grid, roomContext, enteredDoor);
        _necromancer.Teleport(grid.CellToWorld(spawnCell));
        return true;
    }

    private Vector3Int ResolveArrivalCell(RoomGrid grid, RoomContext roomContext, RoomDoor enteredDoor)
    {
        Vector3Int roomCenterCell = grid.WorldToCell(roomContext.transform.position);

        if (!TryResolveArrivalDoorWorldPosition(roomContext.gameObject, enteredDoor, out Vector3 doorWorldPosition))
            return grid.FindClosestWalkableCell(roomCenterCell, roomCenterCell);

        Vector3Int doorCell = grid.WorldToCell(doorWorldPosition);
        Vector3Int bestDirection = GetBestInwardDirection(doorCell, roomCenterCell);

        if (TryGetEnterableAdjacentCell(grid, doorCell, bestDirection, roomCenterCell, out Vector3Int adjacentCell))
            return adjacentCell;

        return grid.FindClosestWalkableCell(doorCell, roomCenterCell);
    }

    private static bool TryResolveArrivalDoorWorldPosition(GameObject roomObject, RoomDoor enteredDoor, out Vector3 doorWorldPosition)
    {
        doorWorldPosition = roomObject.transform.position;

        if (roomObject == null || enteredDoor == null)
            return false;

        GameObject previousRoom = enteredDoor.roomA == roomObject ? enteredDoor.roomB : enteredDoor.roomA;
        if (previousRoom == null)
            return false;

        RoomDoor[] roomDoors = roomObject.GetComponentsInChildren<RoomDoor>(includeInactive: true);
        for (int i = 0; i < roomDoors.Length; i++)
        {
            RoomDoor candidate = roomDoors[i];
            if (candidate == null || candidate == enteredDoor)
                continue;

            bool connectsSameRooms =
                (candidate.roomA == roomObject && candidate.roomB == previousRoom) ||
                (candidate.roomA == previousRoom && candidate.roomB == roomObject);

            if (!connectsSameRooms)
                continue;

            DoorInteractable doorInteractable = candidate.GetComponentInChildren<DoorInteractable>(includeInactive: true);
            doorWorldPosition = doorInteractable != null ? doorInteractable.transform.position : candidate.transform.position;
            return true;
        }

        return false;
    }

    private static Vector3Int GetBestInwardDirection(Vector3Int doorCell, Vector3Int roomCenterCell)
    {
        Vector3 toCenter = (Vector3)(roomCenterCell - doorCell);
        if (toCenter.sqrMagnitude <= Mathf.Epsilon)
            return Vector3Int.zero;

        int bestIndex = 0;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            float score = Vector3.Dot((Vector3)CardinalDirections[i], toCenter);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return CardinalDirections[bestIndex];
    }

    private static bool TryGetEnterableAdjacentCell(
        RoomGrid grid,
        Vector3Int doorCell,
        Vector3Int preferredDirection,
        Vector3Int roomCenterCell,
        out Vector3Int resultCell)
    {
        resultCell = doorCell;

        Vector3 toCenter = (Vector3)(roomCenterCell - doorCell);
        int preferredDirectionIndex = GetCardinalDirectionIndex(preferredDirection);

        int bestScore = int.MinValue;
        bool found = false;

        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector3Int direction = CardinalDirections[i];
            Vector3Int candidateCell = doorCell + direction;
            if (!grid.IsCellEnterable(candidateCell))
                continue;

            int score = Mathf.RoundToInt(Vector3.Dot((Vector3)direction, toCenter) * 1000f);
            if (i == preferredDirectionIndex)
                score += 1;

            if (found && score <= bestScore)
                continue;

            bestScore = score;
            resultCell = candidateCell;
            found = true;
        }

        return found;
    }

    private static int GetCardinalDirectionIndex(Vector3Int direction)
    {
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            if (CardinalDirections[i] == direction)
                return i;
        }

        return -1;
    }
}

using UnityEngine;

public class NecromancerRoomTransitioner : MonoBehaviour
{
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
        MoveNecromancerToRoom(newRoom);
    }

    public bool MoveNecromancerToRoom(GameObject roomObject)
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

        Vector3Int centerCell = grid.WorldToCell(roomObject.transform.position);
        Vector3Int spawnCell = grid.FindClosestWalkableCell(centerCell, centerCell);
        _necromancer.Teleport(grid.CellToWorld(spawnCell));
        return true;
    }
}

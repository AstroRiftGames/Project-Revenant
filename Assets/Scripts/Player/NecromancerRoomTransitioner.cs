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
        if (_necromancer == null)
            _necromancer = FindAnyObjectByType<Necromancer>();

        if (_necromancer == null)
        {
            Debug.LogWarning("[NecromancerRoomTransitioner] No se encontró Necromancer en escena.", this);
            return;
        }

        if (!newRoom.TryGetComponent(out RoomContext roomContext))
        {
            Debug.LogWarning($"[NecromancerRoomTransitioner] '{newRoom.name}' no tiene RoomContext.", this);
            return;
        }

        RoomGrid grid = roomContext.BattleGrid;
        if (grid == null)
        {
            Debug.LogWarning($"[NecromancerRoomTransitioner] RoomContext de '{newRoom.name}' no tiene BattleGrid.", this);
            return;
        }

        _necromancer.SetGrid(grid);

        Vector3Int centerCell = grid.WorldToCell(newRoom.transform.position);
        Vector3Int spawnCell = grid.FindClosestWalkableCell(centerCell, centerCell);
        _necromancer.Teleport(grid.CellToWorld(spawnCell));
    }
}

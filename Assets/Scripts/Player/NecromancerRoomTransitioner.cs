using System.Collections.Generic;
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
        Vector3Int spawnCell = FindClosestWalkableCell(grid, centerCell);
        _necromancer.Teleport(grid.CellToWorld(spawnCell));
    }

    private static Vector3Int FindClosestWalkableCell(RoomGrid grid, Vector3Int origin)
    {
        if (grid.IsCellWalkable(origin))
            return origin;

        var visited = new HashSet<Vector3Int> { origin };
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            Vector3Int cell = queue.Dequeue();

            foreach (Vector3Int neighbor in grid.GetNeighbors(cell))
            {
                if (!grid.IsCellInsideWalkableBounds(neighbor) || !visited.Add(neighbor))
                    continue;

                if (grid.IsCellWalkable(neighbor))
                    return neighbor;

                queue.Enqueue(neighbor);
            }
        }

        return origin;
    }
}

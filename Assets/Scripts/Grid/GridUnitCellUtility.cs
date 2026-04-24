using UnityEngine;

public static class GridUnitCellUtility
{
    public static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        if (grid == null || unit == null)
            return Vector3Int.zero;

        if (unit.TryGetComponent(out UnitMovement movement) && movement.TryGetLogicalCell(out Vector3Int cell))
            return cell;

        return grid.WorldToCell(unit.Position);
    }
}

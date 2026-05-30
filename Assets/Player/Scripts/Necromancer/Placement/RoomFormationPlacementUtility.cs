using UnityEngine;

public static class RoomFormationPlacementUtility
{
    public static bool TryResolveEntryFrame(
        RoomGrid grid,
        RoomContext roomContext,
        RoomDoor enteredDoor,
        Necromancer necromancer,
        out RoomPlacementFrame frame)
    {
        frame = default;

        if (grid == null || roomContext == null)
            return false;

        Vector3Int roomCenterCell = grid.WorldToCell(roomContext.transform.position);
        Vector3Int resolvedArrivalCell =
            RoomTransitionPlacementUtility.ResolveArrivalCell(grid, roomContext, enteredDoor, out Vector3Int forwardDirection);

        if (necromancer == null || !necromancer.TryGetGrid(out RoomGrid necromancerGrid) || !ReferenceEquals(necromancerGrid, grid))
        {
            frame = new RoomPlacementFrame(resolvedArrivalCell, forwardDirection);
            return true;
        }

        Vector3Int necromancerCell = grid.WorldToCell(necromancer.transform.position);
        if (!grid.HasCell(necromancerCell))
        {
            frame = new RoomPlacementFrame(resolvedArrivalCell, forwardDirection);
            return true;
        }

        if (enteredDoor == null)
            forwardDirection = RoomTransitionPlacementUtility.GetBestInwardDirection(necromancerCell, roomCenterCell);

        frame = new RoomPlacementFrame(necromancerCell, forwardDirection);
        return true;
    }

    public static bool TryResolveDeploymentFrame(
        RoomGrid grid,
        RoomContext roomContext,
        Necromancer necromancer,
        out RoomPlacementFrame frame)
    {
        frame = default;

        if (grid == null || roomContext == null)
            return false;

        if (necromancer == null || !necromancer.TryGetGrid(out RoomGrid necromancerGrid) || !ReferenceEquals(necromancerGrid, grid))
            return false;

        Vector3Int anchorCell = grid.WorldToCell(necromancer.transform.position);
        if (!grid.HasCell(anchorCell))
            return false;

        Vector3Int roomCenterCell = grid.WorldToCell(roomContext.transform.position);
        Vector3Int forwardDirection = RoomTransitionPlacementUtility.GetBestInwardDirection(anchorCell, roomCenterCell);
        frame = new RoomPlacementFrame(anchorCell, forwardDirection);
        return true;
    }
}

public sealed class GridCellMovementValidator : IGridCellMovementValidator
{
    public bool CanEnter(RoomGrid grid, GridCellMovementQuery query)
    {
        if (grid == null)
            return false;

        if (!grid.Topology.IsCellStaticallyWalkable(query.Cell))
            return false;

        return !grid.DoesCellBlockMovement(query.Cell, query.MovingOccupant);
    }
}

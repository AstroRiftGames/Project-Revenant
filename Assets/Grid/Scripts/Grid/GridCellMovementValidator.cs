public sealed class GridCellMovementValidator : IGridCellMovementValidator
{
    public bool CanEnter(RoomGrid grid, GridCellMovementQuery query)
    {
        if (grid == null)
            return false;

        if (grid.IsCellHardBlocked(query.Cell))
            return false;

        // If there's an occupant blocking movement, see if it is friendly to the moving occupant.
        // If it is friendly, we treat it as a soft block (traversable/crossable) during pathfinding.
        // Wait, how do we distinguish pathfinding queries from physical step reservation/movement queries?
        // Let's check query.MovingOccupant. If we are in physical movement, we must not occupy the same cell.
        // But A* pathfinding queries pass through neighbors where query.MovingOccupant is the unit itself.
        // Let's inspect: during A* (FindPath), grid.GetNeighbors(current, movingUnit) calls IsStepAllowed, which calls IsCellEnterable(toCell, movingOccupant).
        // During physical reservation, UnitMovement calls grid.OccupancyService.TryReserveCell, which checks IsOccupied(cell, occupant).
        // So IsCellEnterable (CanEnter) is used by A* to find path neighbors!
        // If we allow crossing friendly cells, we can return true in CanEnter if the blocker is friendly to query.MovingOccupant, and let the pathfinder apply a soft cost.
        IGridOccupant blocker = grid.OccupancyService.GetBlockingOccupant(query.Cell, query.MovingOccupant);
        if (blocker != null)
        {
            if (query.MovingOccupant is Creature movingCreature && blocker is Creature blockingCreature)
            {
                if (movingCreature.Team == blockingCreature.Team)
                {
                    // Friendly occupant! We can cross this cell, so it doesn't block.
                    return true;
                }
            }
            return false;
        }

        return !grid.DoesCellBlockMovement(query.Cell, query.MovingOccupant);
    }
}

public interface IGridCellMovementValidator
{
    bool CanEnter(RoomGrid grid, GridCellMovementQuery query);
}

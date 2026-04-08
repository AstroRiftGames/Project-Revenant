using UnityEngine;

public readonly struct GridCellMovementQuery
{
    public GridCellMovementQuery(Vector3Int cell, IGridOccupant movingOccupant = null)
    {
        Cell = cell;
        MovingOccupant = movingOccupant;
    }

    public Vector3Int Cell { get; }
    public IGridOccupant MovingOccupant { get; }
}

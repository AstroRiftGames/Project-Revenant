using UnityEngine;

public interface IGridOccupant
{
    Vector3 OccupancyWorldPosition { get; }
    bool OccupiesCell { get; }
    bool BlocksMovement { get; }
}

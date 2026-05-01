using UnityEngine;

public readonly struct RoomPlacementFrame
{
    public RoomPlacementFrame(Vector3Int anchorCell, Vector3Int forwardDirection)
    {
        AnchorCell = anchorCell;
        ForwardDirection = forwardDirection;
        LateralDirection = new Vector3Int(-forwardDirection.y, forwardDirection.x, 0);
    }

    public Vector3Int AnchorCell { get; }
    public Vector3Int ForwardDirection { get; }
    public Vector3Int LateralDirection { get; }
}

using UnityEngine;

[System.Serializable]
public struct MovementTileFeedbackState
{
    public MovementTileFeedbackVisualState VisualState;
    public Vector3Int Cell;

    public bool IsVisible => VisualState != MovementTileFeedbackVisualState.Hidden;

    public static MovementTileFeedbackState Hidden =>
        new()
        {
            VisualState = MovementTileFeedbackVisualState.Hidden,
            Cell = Vector3Int.zero
        };

    public static MovementTileFeedbackState Create(MovementTileFeedbackVisualState visualState, Vector3Int cell)
    {
        return new MovementTileFeedbackState
        {
            VisualState = visualState,
            Cell = cell
        };
    }
}

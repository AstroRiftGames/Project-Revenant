using UnityEngine;

[DisallowMultipleComponent]
public class MovementTileFeedbackView : MonoBehaviour
{
    [SerializeField] private GameObject _hoverValidVisual;
    [SerializeField] private GameObject _hoverInvalidVisual;
    [SerializeField] private GameObject _selectedValidVisual;

    public void Render(RoomGrid grid, MovementTileFeedbackState hoverState, MovementTileFeedbackState selectionState)
    {
        if (grid == null)
        {
            HideAll();
            return;
        }

        RenderHover(grid, hoverState);
        RenderSelection(grid, selectionState);
    }

    public void HideAll()
    {
        SetVisualActive(_hoverValidVisual, false);
        SetVisualActive(_hoverInvalidVisual, false);
        SetVisualActive(_selectedValidVisual, false);
    }

    private void RenderHover(RoomGrid grid, MovementTileFeedbackState state)
    {
        SetVisual(_hoverValidVisual, grid, state, state.VisualState == MovementTileFeedbackVisualState.HoverValid);
        SetVisual(_hoverInvalidVisual, grid, state, state.VisualState == MovementTileFeedbackVisualState.HoverInvalid);
    }

    private void RenderSelection(RoomGrid grid, MovementTileFeedbackState state)
    {
        SetVisual(_selectedValidVisual, grid, state, state.VisualState == MovementTileFeedbackVisualState.SelectedValid);
    }

    private static void SetVisual(GameObject visual, RoomGrid grid, MovementTileFeedbackState state, bool shouldShow)
    {
        if (visual == null)
            return;

        if (shouldShow && grid != null && state.IsVisible)
        {
            Transform visualTransform = visual.transform;
            Vector3 position = grid.CellToWorld(state.Cell);
            position.z = visualTransform.position.z;
            visualTransform.position = position;
        }

        SetVisualActive(visual, shouldShow && state.IsVisible);
    }

    private static void SetVisualActive(GameObject visual, bool isActive)
    {
        if (visual != null && visual.activeSelf != isActive)
            visual.SetActive(isActive);
    }
}

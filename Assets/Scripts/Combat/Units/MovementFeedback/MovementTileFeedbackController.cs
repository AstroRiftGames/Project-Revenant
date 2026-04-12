using UnityEngine;

[DisallowMultipleComponent]
public class MovementTileFeedbackController : MonoBehaviour, IRoomContextUnitComponent
{
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private MovementTileFeedbackView _view;

    private MovementTileFeedbackState _hoverState = MovementTileFeedbackState.Hidden;
    private MovementTileFeedbackState _selectionState = MovementTileFeedbackState.Hidden;

    private void Awake()
    {
        if (_view == null)
            _view = GetComponentInChildren<MovementTileFeedbackView>(includeInactive: true);

        RefreshView();
    }

    public void SetGrid(RoomGrid grid)
    {
        _grid = grid;
        RefreshView();
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        SetGrid(roomContext != null ? roomContext.BattleGrid : null);
    }

    public void ShowHover(Vector3Int cell, bool isValidDestination)
    {
        _hoverState = MovementTileFeedbackState.Create(
            isValidDestination
                ? MovementTileFeedbackVisualState.HoverValid
                : MovementTileFeedbackVisualState.HoverInvalid,
            cell);

        RefreshView();
    }

    public void HideHover()
    {
        if (!_hoverState.IsVisible)
            return;

        _hoverState = MovementTileFeedbackState.Hidden;
        RefreshView();
    }

    public void SetSelection(Vector3Int cell)
    {
        _selectionState = MovementTileFeedbackState.Create(
            MovementTileFeedbackVisualState.SelectedValid,
            cell);

        RefreshView();
    }

    public void ClearSelection()
    {
        if (!_selectionState.IsVisible)
            return;

        _selectionState = MovementTileFeedbackState.Hidden;
        RefreshView();
    }

    public void HideAll()
    {
        _hoverState = MovementTileFeedbackState.Hidden;
        _selectionState = MovementTileFeedbackState.Hidden;
        RefreshView();
    }

    private void RefreshView()
    {
        if (_view == null)
            return;

        _view.Render(_grid, _hoverState, _selectionState);
    }
}

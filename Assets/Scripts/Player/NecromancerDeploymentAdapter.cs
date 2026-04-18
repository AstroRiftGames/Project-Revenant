using Selection.Interfaces;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Necromancer))]
public class NecromancerDeploymentAdapter : MonoBehaviour
{
    [SerializeField] private Necromancer _necromancer;
    [SerializeField] private Camera _inputCamera;
    [SerializeField] private LayerMask _selectableLayer;
    [SerializeField] private bool _debugLogs;

    private Camera _mainCamera;
    private Unit _selectedUnit;
    private ISelectable _selectedSelectable;
    private MovementTileFeedbackController _movementTileFeedback;
    private Unit _movementLockedUnit;
    private Vector3Int _lockedDestinationCell;
    private bool _hasLockedDestinationCell;

    private void Awake()
    {
        _necromancer ??= GetComponent<Necromancer>();
        _movementTileFeedback = GetComponent<MovementTileFeedbackController>();
    }

    private void OnDisable()
    {
        ClearSelection();
        ClearDestinationFeedback();
    }

    private void Update()
    {
        if (_necromancer == null)
            return;

        RefreshMovementLock();

        if (!TryResolveDeploymentController(out CombatRoomController deploymentController))
        {
            ClearSelection();
            ClearDestinationFeedback();
            return;
        }

        ResolveRuntimeCamera();

        if (_mainCamera == null || !Input.GetMouseButtonDown(0))
            return;

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        if (IsMovementLockActive())
        {
            LogDebug(
                $"[{nameof(NecromancerDeploymentAdapter)}] Deployment input ignored while '{_movementLockedUnit.name}' " +
                $"is still moving to {_lockedDestinationCell}.");
            return;
        }

        if (TrySelectAllyUnderPointer(mouseWorld, deploymentController))
            return;

        if (_selectedUnit == null)
            return;

        RoomGrid grid = deploymentController.RoomContext != null ? deploymentController.RoomContext.RoomGrid : null;
        if (grid == null)
            return;

        Vector3Int targetCell = grid.WorldToCell(mouseWorld);
        bool moved = deploymentController.TryMoveDeployedAlly(_selectedUnit, targetCell);
        if (moved)
            LockDestinationUntilArrival(_selectedUnit, targetCell);

        LogDebug(
            $"[{nameof(NecromancerDeploymentAdapter)}] Deployment move requested for '{_selectedUnit.name}' to {targetCell}. " +
            $"Moved: {moved}.");
    }

    private bool TryResolveDeploymentController(out CombatRoomController deploymentController)
    {
        deploymentController = null;
        if (!_necromancer.TryGetGrid(out RoomGrid grid) || grid == null)
            return false;

        RoomContext roomContext = grid.GetComponentInParent<RoomContext>(includeInactive: true);
        deploymentController = roomContext != null ? roomContext.CombatController : null;
        return deploymentController != null && deploymentController.CanDeployUnits;
    }

    private bool TrySelectAllyUnderPointer(Vector3 mouseWorld, CombatRoomController deploymentController)
    {
        Collider2D hitCollider = TryResolveSelectableCollider(mouseWorld);
        if (hitCollider == null)
            return false;

        Unit unit = hitCollider.GetComponentInParent<Unit>();
        if (unit == null ||
            unit.Team != UnitTeam.NecromancerAlly ||
            !ReferenceEquals(unit.RoomContext, deploymentController.RoomContext))
        {
            return false;
        }

        SetSelectedUnit(unit);
        LogDebug($"[{nameof(NecromancerDeploymentAdapter)}] Selected deployment unit '{unit.name}'.");
        return true;
    }

    private Collider2D TryResolveSelectableCollider(Vector3 mouseWorld)
    {
        if (_selectableLayer.value != 0)
        {
            RaycastHit2D layeredHit = Physics2D.Raycast(mouseWorld, Vector2.zero, Mathf.Infinity, _selectableLayer);
            if (layeredHit.collider != null)
                return layeredHit.collider;
        }
        else
        {
            _selectableLayer = LayerMask.GetMask("Selectable");
            if (_selectableLayer.value != 0)
            {
                RaycastHit2D defaultLayerHit = Physics2D.Raycast(mouseWorld, Vector2.zero, Mathf.Infinity, _selectableLayer);
                if (defaultLayerHit.collider != null)
                    return defaultLayerHit.collider;
            }
        }

        return Physics2D.OverlapPoint(mouseWorld);
    }

    private void SetSelectedUnit(Unit unit)
    {
        if (ReferenceEquals(_selectedUnit, unit))
            return;

        ClearSelection();

        _selectedUnit = unit;
        _selectedSelectable = unit;
        _selectedSelectable?.Select();
    }

    private void ClearSelection()
    {
        _selectedSelectable?.Deselect();
        _selectedSelectable = null;
        _selectedUnit = null;
    }

    private void LockDestinationUntilArrival(Unit unit, Vector3Int destinationCell)
    {
        _movementLockedUnit = unit;
        _hasLockedDestinationCell = true;
        _lockedDestinationCell = destinationCell;
        _movementTileFeedback?.SetSelection(destinationCell);
    }

    private void RefreshMovementLock()
    {
        if (_movementLockedUnit == null)
            return;

        UnitMovement lockedMovement = _movementLockedUnit.GetComponent<UnitMovement>();
        if (lockedMovement != null && lockedMovement.IsMoving)
            return;

        _movementLockedUnit = null;
        ClearDestinationFeedback();
    }

    private bool IsMovementLockActive()
    {
        return _movementLockedUnit != null;
    }

    private void ClearDestinationFeedback()
    {
        if (!_hasLockedDestinationCell)
            return;

        _hasLockedDestinationCell = false;
        _lockedDestinationCell = Vector3Int.zero;
        _movementTileFeedback?.ClearSelection();
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }

    private void ResolveRuntimeCamera()
    {
        if (_inputCamera != null)
        {
            _mainCamera = _inputCamera;
            return;
        }

        if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
            _mainCamera = Camera.main;
    }
}

using Selection.Interfaces;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Necromancer))]
public class NecromancerDeploymentController : MonoBehaviour
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
    private CombatRoomController _currentDeploymentController;
    private bool _isSubscribedToController;
    private Vector3Int _lockedDestinationCell;
    private bool _hasLockedDestinationCell;

    private void Awake()
    {
        _necromancer ??= GetComponent<Necromancer>();
        _movementTileFeedback = GetComponent<MovementTileFeedbackController>();
    }

    private void OnDisable()
    {
        UnsubscribeFromDeploymentController();
        LifeController.OnUnitDied -= HandleUnitDied;
        ClearSelection();
        ClearDestinationFeedback();
    }

    private void OnEnable()
    {
        LifeController.OnUnitDied += HandleUnitDied;
    }

    private void Update()
    {
        if (_necromancer == null)
            return;

        RefreshMovementLock();

        if (!TryResolveDeploymentController(out CombatRoomController deploymentController))
        {
            UnsubscribeFromDeploymentController();
            ClearSelection();
            ClearDestinationFeedback();
            return;
        }

        RefreshDeploymentControllerSubscription(deploymentController);

        ResolveRuntimeCamera();

        if (_mainCamera == null || !Input.GetMouseButtonDown(0))
            return;

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        if (IsMovementLockActive())
        {
            LogDebug(
                $"[{nameof(NecromancerDeploymentController)}] Deployment input ignored while '{_movementLockedUnit.name}' " +
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
            $"[{nameof(NecromancerDeploymentController)}] Deployment move requested for '{_selectedUnit.name}' to {targetCell}. " +
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
        LogDebug($"[{nameof(NecromancerDeploymentController)}] Selected deployment unit '{unit.name}'.");
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
        _selectedSelectable.OnSelectionInvalidated += HandleSelectedInvalidated;
        _selectedSelectable?.Select();
    }

    private void ClearSelection()
    {
        if (_selectedSelectable != null)
            _selectedSelectable.OnSelectionInvalidated -= HandleSelectedInvalidated;

        if (_selectedUnit != null)
            LogDebug($"[{nameof(NecromancerDeploymentController)}] Cleared deployment selection for '{_selectedUnit.name}'.");

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

    private void RefreshDeploymentControllerSubscription(CombatRoomController deploymentController)
    {
        if (ReferenceEquals(_currentDeploymentController, deploymentController))
            return;

        UnsubscribeFromDeploymentController();
        _currentDeploymentController = deploymentController;

        if (_currentDeploymentController == null)
            return;

        _currentDeploymentController.CombatStarted += HandleCombatStarted;
        _currentDeploymentController.StateChanged += HandleDeploymentStateChanged;
        _isSubscribedToController = true;
    }

    private void UnsubscribeFromDeploymentController()
    {
        if (!_isSubscribedToController || _currentDeploymentController == null)
            return;

        _currentDeploymentController.CombatStarted -= HandleCombatStarted;
        _currentDeploymentController.StateChanged -= HandleDeploymentStateChanged;
        _currentDeploymentController = null;
        _isSubscribedToController = false;
    }

    private void HandleCombatStarted(CombatRoomController _)
    {
        ClearDeploymentVisualState();
    }

    private void HandleDeploymentStateChanged(CombatRoomController _, CombatRoomState state)
    {
        if (state == CombatRoomState.Deployment)
            return;

        ClearDeploymentVisualState();
    }

    private void HandleSelectedInvalidated(ISelectable selectable)
    {
        if (!ReferenceEquals(_selectedSelectable, selectable))
            return;

        LogDebug($"[{nameof(NecromancerDeploymentController)}] Selected unit invalidated. Clearing selection feedback.");
        ClearSelection();
    }

    private void HandleUnitDied(Unit unit)
    {
        if (unit == null)
            return;

        if (ReferenceEquals(unit, _selectedUnit))
            ClearSelection();

        if (ReferenceEquals(unit, _movementLockedUnit))
        {
            _movementLockedUnit = null;
            ClearDestinationFeedback();
        }
    }

    private void ClearDeploymentVisualState()
    {
        ClearSelection();
        ClearDestinationFeedback();
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

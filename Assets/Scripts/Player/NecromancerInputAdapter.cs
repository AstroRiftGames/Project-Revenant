using UnityEngine;
using Selection.Interfaces;
using Selection.Core;

[DisallowMultipleComponent]
[RequireComponent(typeof(Necromancer))]
public class NecromancerInputAdapter : MonoBehaviour
{
    private readonly struct PointerContractContext
    {
        public PointerContractContext(bool isOverSelectable, bool isOverInteractable)
        {
            IsOverSelectable = isOverSelectable;
            IsOverInteractable = isOverInteractable;
        }

        public bool IsOverSelectable { get; }
        public bool IsOverInteractable { get; }
        public bool ShouldBlockMovementCommand => IsOverSelectable;
        public bool ShouldBlockCancelCommand => IsOverInteractable;
    }

    private readonly struct ManualMovementPointerContext
    {
        public ManualMovementPointerContext(
            Vector3Int hoveredCell,
            bool isInsideGrid,
            bool isOverNavigableSurface,
            PointerContractContext contractContext)
        {
            HoveredCell = hoveredCell;
            IsInsideGrid = isInsideGrid;
            IsOverNavigableSurface = isOverNavigableSurface;
            ContractContext = contractContext;
        }

        public Vector3Int HoveredCell { get; }
        public bool IsInsideGrid { get; }
        public bool IsOverNavigableSurface { get; }
        public bool IsOverBlockedWorldSurface => IsInsideGrid && !IsOverNavigableSurface;
        public PointerContractContext ContractContext { get; }
        public bool CanIssueMovementCommand => IsOverNavigableSurface && !ContractContext.ShouldBlockMovementCommand;
    }

    [SerializeField] private Necromancer _necromancer;
    [SerializeField] private Camera _inputCamera;
    [SerializeField] private LayerMask _selectionBlockerLayer;
    [SerializeField] private LayerMask _interactionBlockerLayer;
    [SerializeField] private bool _cancelSelectionOnRightClick = true;
    [SerializeField] private bool _cancelSelectionOnEscape = true;

    private Camera _mainCamera;

    private void Awake()
    {
        _necromancer ??= GetComponent<Necromancer>();
        _mainCamera = _inputCamera != null ? _inputCamera : Camera.main;
    }

    private void OnValidate()
    {
        if (_selectionBlockerLayer.value == 0)
            _selectionBlockerLayer = LayerMask.GetMask("Selectable");

        if (_interactionBlockerLayer.value == 0)
            _interactionBlockerLayer = LayerMask.GetMask("Interactable");
    }

    private void Update()
    {
        if (_necromancer == null)
            return;

        if (IsManualMovementBlockedByEncounterState())
        {
            _necromancer.HandleManualPointerExitedGrid();
            TryHandleCancelInput(new PointerContractContext(false, false));
            return;
        }

        if (!_necromancer.TryGetGrid(out RoomGrid grid))
        {
            _necromancer.HandleManualInputUnavailable();
            return;
        }

        if (_mainCamera == null)
            _mainCamera = _inputCamera != null ? _inputCamera : Camera.main;

        if (_mainCamera == null)
        {
            _necromancer.HandleManualInputCameraUnavailable();
            return;
        }

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        ManualMovementPointerContext pointerContext = ResolveManualMovementPointerContext(grid, mouseWorld);
        if (!pointerContext.IsInsideGrid)
        {
            _necromancer.HandleManualPointerExitedGrid();
            TryHandleCancelInput(pointerContext.ContractContext);
            return;
        }

        _necromancer.UpdateManualHoveredCell(pointerContext.HoveredCell, pointerContext.IsOverNavigableSurface);

        TryHandleCancelInput(pointerContext.ContractContext);

        if (!ShouldIssueMovementCommand(pointerContext))
            return;

        _necromancer.TrySetManualDestination(pointerContext.HoveredCell, pointerContext.IsOverNavigableSurface);
    }

    private bool ShouldIssueMovementCommand(ManualMovementPointerContext pointerContext)
    {
        return Input.GetMouseButtonDown(0) && pointerContext.CanIssueMovementCommand;
    }

    private void TryHandleCancelInput(PointerContractContext pointerContract)
    {
        bool cancelRequested =
            (_cancelSelectionOnRightClick && Input.GetMouseButtonDown(1) && !pointerContract.ShouldBlockCancelCommand) ||
            (_cancelSelectionOnEscape && Input.GetKeyDown(KeyCode.Escape));

        if (!cancelRequested)
            return;

        _necromancer.CancelManualMovement();
    }

    private PointerContractContext ResolvePointerContractContext(Vector3 mouseWorld)
    {
        bool isOverSelectable = TryResolvePointerTarget(mouseWorld, _selectionBlockerLayer.value, out ISelectable _);
        bool isOverInteractable = PointerInteractableResolver.TryResolveFromWorldPoint(
            mouseWorld,
            _interactionBlockerLayer.value,
            out RaycastHit2D _,
            out IInteractable _);
        return new PointerContractContext(isOverSelectable, isOverInteractable);
    }

    private ManualMovementPointerContext ResolveManualMovementPointerContext(RoomGrid grid, Vector3 mouseWorld)
    {
        Vector3Int hoveredCell = grid.WorldToCell(mouseWorld);
        bool isInsideGrid = grid.HasCell(hoveredCell);
        bool isOverNavigableSurface = isInsideGrid && grid.IsCellEnterable(hoveredCell);
        PointerContractContext contractContext = ResolvePointerContractContext(mouseWorld);

        return new ManualMovementPointerContext(
            hoveredCell,
            isInsideGrid,
            isOverNavigableSurface,
            contractContext);
    }

    private bool IsManualMovementBlockedByEncounterState()
    {
        if (_necromancer == null || !_necromancer.TryGetGrid(out RoomGrid grid) || grid == null)
            return false;

        RoomContext roomContext = grid.GetComponentInParent<RoomContext>(includeInactive: true);
        CombatRoomController combatController = roomContext != null ? roomContext.CombatController : null;
        return combatController != null && combatController.IsCombatRoom && !combatController.IsResolved;
    }

    private static bool TryResolvePointerTarget<TContract>(Vector3 worldPosition, int layerMask, out TContract contract)
        where TContract : class
    {
        contract = null;

        if (layerMask == 0)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, layerMask);
        if (hit.collider == null)
            return false;

        contract = hit.collider.GetComponentInParent<TContract>();
        return contract != null;
    }
}

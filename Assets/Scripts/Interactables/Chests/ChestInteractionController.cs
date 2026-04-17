using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ChestState))]
public class ChestInteractionController : MonoBehaviour, IInteractable, IGridOccupant, IRoomContextComponent
{
    private const int RequiredAdjacencyDistance = 1;

    [SerializeField] private RoomGrid _grid;
    [SerializeField] private ChestState _state;
    [SerializeField] private bool _blocksMovement = true;

    private readonly List<MonoBehaviour> _componentBuffer = new();

    private RoomContext _roomContext;
    private Necromancer _necromancer;
    private IChestContentResolver _contentResolver;
    private bool _isOccupancyRegistered;
    private bool _isInteractionAvailable;

    public event Action<bool> OnInteractionAvailabilityChanged;

    public bool IsInteractionAvailable => _isInteractionAvailable;
    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;

    private void Awake()
    {
        _state ??= GetComponent<ChestState>();
        ResolveContentResolver();
    }

    private void OnEnable()
    {
        if (_state != null)
            _state.OnOpenedStateChanged += HandleOpenedStateChanged;

        TryRegisterOccupancy();
        RefreshInteractionAvailability(forceEvent: true);
    }

    private void Start()
    {
        TryRegisterOccupancy();
        RefreshInteractionAvailability(forceEvent: true);
    }

    private void Update()
    {
        RefreshInteractionAvailability(forceEvent: false);
    }

    private void OnDisable()
    {
        if (_state != null)
            _state.OnOpenedStateChanged -= HandleOpenedStateChanged;

        ReleaseOccupancy();
        SetInteractionAvailability(false, forceEvent: true);
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        _roomContext = roomContext;
        _grid = roomContext != null ? roomContext.RoomGrid : _grid;
        _necromancer = null;

        TryRegisterOccupancy();
        RefreshInteractionAvailability(forceEvent: true);
    }

    [ContextMenu("Interact")]
    public void Interact()
    {
        if (!CanInteract())
            return;

        Debug.Log($"[ChestInteractionController] Interacting with chest '{name}'.", this);
        OpenChest();
    }

    private void OpenChest()
    {
        if (!TryOpenChest())
            return;

        if (_contentResolver == null)
        {
            Debug.LogWarning($"[ChestInteractionController] '{name}' opened without a content resolver.", this);
            return;
        }

        _contentResolver.ResolveContent(CreateContentSpawnContext());
    }

    private bool TryOpenChest()
    {
        return _state != null && _state.TryOpen();
    }

    private ChestContentSpawnContext CreateContentSpawnContext()
    {
        return new ChestContentSpawnContext(
            _roomContext,
            _grid,
            _state,
            transform,
            ResolveChestTopPosition());
    }

    private void HandleOpenedStateChanged(bool isOpened)
    {
        if (isOpened)
            SetInteractionAvailability(false, forceEvent: true);
    }

    private bool CanInteract()
    {
        if (_state == null || !_state.CanOpen)
            return false;

        return IsInteractionAvailable;
    }

    private void RefreshInteractionAvailability(bool forceEvent)
    {
        _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);

        bool shouldBeAvailable =
            _state != null &&
            _state.CanOpen &&
            GridInteractionAvailability.IsNecromancerAdjacent(_grid, _necromancer, transform.position);

        SetInteractionAvailability(shouldBeAvailable, forceEvent);
    }

    private void SetInteractionAvailability(bool isAvailable, bool forceEvent)
    {
        if (!forceEvent && _isInteractionAvailable == isAvailable)
            return;

        _isInteractionAvailable = isAvailable;
        OnInteractionAvailabilityChanged?.Invoke(_isInteractionAvailable);
    }

    private Vector3 ResolveChestTopPosition()
    {
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
            return transform.position;

        Bounds bounds = spriteRenderer.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, transform.position.z);
    }

    private void ResolveContentResolver()
    {
        _contentResolver = null;
        _componentBuffer.Clear();
        GetComponents(_componentBuffer);

        for (int i = 0; i < _componentBuffer.Count; i++)
        {
            if (_componentBuffer[i] is IChestContentResolver resolver)
            {
                _contentResolver = resolver;
                return;
            }
        }
    }

    private void TryRegisterOccupancy()
    {
        if (_isOccupancyRegistered || _grid == null)
            return;

        _grid.OccupancyService.RegisterOccupant(this);
        _isOccupancyRegistered = true;
    }

    private void ReleaseOccupancy()
    {
        if (!_isOccupancyRegistered || _grid == null)
            return;

        _grid.OccupancyService.ReleaseOccupant(this);
        _isOccupancyRegistered = false;
    }
}

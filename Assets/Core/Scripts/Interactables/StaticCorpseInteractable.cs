using System;
using UnityEngine;

[DisallowMultipleComponent]
public class StaticCorpseInteractable : MonoBehaviour, IInteractable, IGridOccupant, IRoomContextComponent
{
    [SerializeField] private UnitData _unitToRecruit;
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private Transform _cellAnchor;
    
    private bool _hasInteracted;
    private bool _isOccupancyRegistered;
    private Necromancer _necromancer;
    private bool _isInteractionAvailable;

    public event Action<bool> OnInteractionAvailabilityChanged;

    public bool IsInteractionAvailable => _isInteractionAvailable;
    public Vector3 OccupancyWorldPosition => GetAnchorWorldPosition();
    public bool OccupiesCell => gameObject.activeInHierarchy && !_hasInteracted;
    public bool BlocksMovement => true;

    private void OnEnable()
    {
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
        ReleaseOccupancy();
        SetInteractionAvailability(false, forceEvent: true);
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        _grid = RoomGridResolver.ResolveFromContext(roomContext) ?? _grid;
        _necromancer = null;

        TryRegisterOccupancy();
        RefreshInteractionAvailability(forceEvent: true);
    }

    public void Interact()
    {
        if (!IsInteractionAvailable)
            return;

        if (NecromancerParty.Instance != null)
        {
            bool recruited = NecromancerParty.Instance.TryAddMember(_unitToRecruit);
            if (recruited)
            {
                _hasInteracted = true;
                ReleaseOccupancy();
                SetInteractionAvailability(false, forceEvent: true);
                
                // Disappear after interaction
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[{nameof(StaticCorpseInteractable)}] Could not add {_unitToRecruit.displayName} to party. Party might be full.", this);
            }
        }
        else
        {
            Debug.LogError($"[{nameof(StaticCorpseInteractable)}] NecromancerParty Instance is null. Cannot recruit.", this);
        }
    }

    private void RefreshInteractionAvailability(bool forceEvent)
    {
        _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);

        bool shouldBeAvailable =
            !_hasInteracted &&
            _unitToRecruit != null &&
            GridInteractionAvailability.IsNecromancerAdjacent(_grid, _necromancer, GetAnchorWorldPosition());

        SetInteractionAvailability(shouldBeAvailable, forceEvent);
    }

    private void SetInteractionAvailability(bool isAvailable, bool forceEvent)
    {
        if (!forceEvent && _isInteractionAvailable == isAvailable)
            return;

        _isInteractionAvailable = isAvailable;
        OnInteractionAvailabilityChanged?.Invoke(_isInteractionAvailable);
    }

    private Vector3 GetAnchorWorldPosition()
    {
        if (_cellAnchor != null)
            return _cellAnchor.position;

        return transform.position;
    }

    private void TryRegisterOccupancy()
    {
        if (_grid != null && OccupiesCell)
            _isOccupancyRegistered = StaticGridOccupancyUtility.TryRegister(_grid, this, _isOccupancyRegistered);
    }

    private void ReleaseOccupancy()
    {
        if (_grid != null)
            _isOccupancyRegistered = StaticGridOccupancyUtility.Release(_grid, this, _isOccupancyRegistered);
    }
}

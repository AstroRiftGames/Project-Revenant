using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRoomContextUnitComponent
{
    void IntegrateWithRoom(RoomContext roomContext);
}

[RequireComponent(typeof(LifeController))]
public class Unit : Creature, IGridOccupant
{
    [SerializeField] private UnitData _unitData;
    [SerializeField] private MonoBehaviour _actionSource;

    private RoomContext _roomContext;
    private IBasicAction _resolvedAction;
    private readonly List<MonoBehaviour> _roomContextComponentsBuffer = new();

    protected override void Awake()
    {
        base.Awake();

        if (_unitData != null)
            Initialize(_unitData);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RoomContext context = GetComponentInParent<RoomContext>();
        if (context != null)
            context.RegisterUnit(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_roomContext != null)
            _roomContext.UnregisterUnit(this);
    }

    public RoomContext RoomContext => _roomContext;
    public IBasicAction Action => _resolvedAction ??= ResolveAction();
    public bool IsDpsMelee => Role == UnitRole.DPS && CombatStyle != UnitCombatStyle.Ranged;
    public bool IsDpsRanged => Role == UnitRole.DPS && CombatStyle == UnitCombatStyle.Ranged;
    public bool WantsToHoldSpacing => Role == UnitRole.Support || IsDpsRanged;
    public Vector3 OccupancyWorldPosition
    {
        get
        {
            UnitMovement movement = GetComponent<UnitMovement>();
            if (movement != null && movement.TryGetLogicalWorldPosition(out Vector3 logicalWorldPosition))
                return logicalWorldPosition;

            return transform.position;
        }
    }
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => true;

    public void AssignRoomContext(RoomContext context)
    {
        _roomContext = context;
    }

    public void IntegrateIntoRoom(RoomContext roomContext)
    {
        AssignRoomContext(roomContext);

        _roomContextComponentsBuffer.Clear();
        GetComponents(_roomContextComponentsBuffer);

        for (int i = 0; i < _roomContextComponentsBuffer.Count; i++)
        {
            MonoBehaviour component = _roomContextComponentsBuffer[i];
            if (component is IRoomContextUnitComponent roomContextComponent)
                roomContextComponent.IntegrateWithRoom(roomContext);
        }
    }

    public IReadOnlyList<Unit> GetRoomUnits()
    {
        return TargetingCandidateProvider.GetRoomCandidates(this);
    }

    public int GetPreferredDistance(IBasicAction action)
    {
        if (action == null)
            return 0;

        return Mathf.Max(0, Mathf.Min(action.PreferredDistanceInCells, action.RangeInCells));
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        RoomGrid grid = _roomContext != null
            ? _roomContext.RoomGrid
            : GetComponentInParent<RoomGrid>(includeInactive: true);

        if (grid == null)
            return;

        UnitMovement movement = GetComponent<UnitMovement>();
        if (movement != null)
        {
            movement.SetGrid(grid);
            movement.ForceSyncToWorldPosition(transform.position);
            return;
        }

        transform.position = GridNavigationUtility.ResolvePlacementWorldPosition(grid, transform.position, this);
    }

    private IBasicAction ResolveAction()
    {
        if (_actionSource is IBasicAction explicitAction)
            return explicitAction;

        BasicUnitAction attachedAction = GetComponent<BasicUnitAction>();
        if (attachedAction != null)
            return attachedAction;

        UnitCombat combat = GetComponent<UnitCombat>();
        if (combat == null)
            return null;

        return new CombatAction(this, combat, Role == UnitRole.Support);
    }
}

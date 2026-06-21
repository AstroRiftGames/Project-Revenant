using System;
using System.Collections.Generic;
using UnityEngine;

public interface IRoomContextUnitComponent
{
    void IntegrateWithRoom(RoomContext roomContext);
}

public enum UnitOperationalState
{
    Idle,
    Moving,
    Attacking,
    Casting,
    CrowdControl,
    Dead
}

[DisallowMultipleComponent]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(UnitDeathHandler))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitCombat))]
[RequireComponent(typeof(TargetingStrategy))]
[RequireComponent(typeof(UnitBrain))]
public class Unit : Creature, IGridOccupant
{
    [SerializeField] private UnitData _unitData;
    [SerializeField] private MonoBehaviour _actionSource;

    private RoomContext _roomContext;
    private IBasicAction _resolvedAction;
    private UnitMovement _movement;
    private SkillCaster _operationalSkillCaster;
    private bool _isExecutingBasicAction;
    private readonly List<MonoBehaviour> _roomContextComponentsBuffer = new();

    protected override void Awake()
    {
        base.Awake();
        _movement = GetComponent<UnitMovement>();
        _operationalSkillCaster = GetComponent<SkillCaster>();

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
        if (_roomContext != null && _roomContext.RoomGrid != null && _roomContext.RoomGrid.OccupancyService != null)
        {
            _roomContext.RoomGrid.OccupancyService.ReleaseOccupant(this);
        }
        else
        {
            RoomGrid grid = GetComponentInParent<RoomGrid>(includeInactive: true);
            if (grid != null && grid.OccupancyService != null)
            {
                grid.OccupancyService.ReleaseOccupant(this);
            }
        }

        if (_roomContext != null)
            _roomContext.UnregisterUnit(this);
    }

    private void OnDestroy()
    {
        if (_roomContext != null && _roomContext.RoomGrid != null && _roomContext.RoomGrid.OccupancyService != null)
        {
            _roomContext.RoomGrid.OccupancyService.ReleaseOccupant(this);
        }
        else
        {
            RoomGrid grid = GetComponentInParent<RoomGrid>(includeInactive: true);
            if (grid != null && grid.OccupancyService != null)
            {
                grid.OccupancyService.ReleaseOccupant(this);
            }
        }
    }

    public RoomContext RoomContext => _roomContext;
    public IBasicAction Action => _resolvedAction ??= ResolveAction();
    public UnitOperationalState OperationalState => ResolveOperationalState();
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
    // Only alive units occupy cells as regular occupants.
    // Recruitable corpses block via persistent blocker, not as IGridOccupant.
    public bool OccupiesCell => IsAlive && LifecycleState == UnitLifecycleState.Alive && gameObject.activeInHierarchy;
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
        if (RoomContext != null)
            return RoomContext.Units;

        return Array.Empty<Unit>();
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

    public void BeginBasicActionExecution()
    {
        _isExecutingBasicAction = true;
    }

    public void EndBasicActionExecution()
    {
        _isExecutingBasicAction = false;
    }

    // Debug/manual validation entry point for room-backed combat debug tools.
    // Reuses the live basic action pipeline without UnitBrain orchestration.
    public bool TryBasicActionForDebug(Unit forcedTarget)
    {
        IBasicAction action = Action;
        if (action == null || !IsAlive || LifecycleState != UnitLifecycleState.Alive)
            return false;

        BeginBasicActionExecution();
        try
        {
            return action.Execute(this, forcedTarget);
        }
        finally
        {
            EndBasicActionExecution();
        }
    }

    // Alias kept explicit for debug tool discoverability.
    public bool TryBasicAttackForDebug(Unit forcedTarget)
    {
        return TryBasicActionForDebug(forcedTarget);
    }

    private UnitOperationalState ResolveOperationalState()
    {
        if (!IsAlive || LifecycleState != UnitLifecycleState.Alive)
            return UnitOperationalState.Dead;

        if (StatusEffects != null && !StatusEffects.CanAct)
            return UnitOperationalState.CrowdControl;

        if (_operationalSkillCaster != null && _operationalSkillCaster.IsCasting)
            return UnitOperationalState.Casting;

        if (_isExecutingBasicAction)
            return UnitOperationalState.Attacking;

        if (_movement != null && _movement.IsMoving)
            return UnitOperationalState.Moving;

        return UnitOperationalState.Idle;
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

#if UNITY_EDITOR
    // Editor-only hook for Creature Variant Lab. Sets _unitData and fully initializes
    // the unit so it can function at runtime without going through the normal Awake path.
    public void ForceSetUnitDataForEditor(UnitData data)
    {
        if (data == null)
        {
            Debug.LogError("[Unit] ForceSetUnitDataForEditor called with null data. Initialization skipped.");
            return;
        }
        _unitData = data;
        ForceInitializeForEditor(data);
    }
#endif
}

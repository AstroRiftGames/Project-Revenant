using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeController))]
public class Unit : Creature
{
    [SerializeField] private UnitData _unitData;
    [SerializeField] private MonoBehaviour _actionSource;

    private RoomContext _roomContext;
    private IAction _resolvedAction;
    private readonly List<IUnit> _visibleUnitsBuffer = new();
    private readonly List<Unit> _alliedUnitsBuffer = new();
    private readonly List<IUnit> _visibleHostilesBuffer = new();
    private readonly List<Unit> _hostileUnitsBuffer = new();

    protected override void Awake()
    {
        base.Awake();

        if (_unitData != null)
            Initialize(_unitData);
    }

    private void OnEnable()
    {
        RoomContext context = GetComponentInParent<RoomContext>();
        if (context != null)
            context.RegisterUnit(this);
    }

    private void OnDisable()
    {
        if (_roomContext != null)
            _roomContext.UnregisterUnit(this);
    }

    public RoomContext RoomContext => _roomContext;
    public IAction Action => _resolvedAction ??= ResolveAction();

    public void AssignRoomContext(RoomContext context)
    {
        _roomContext = context;
    }

    public List<IUnit> GetVisibleUnitsInScene()
    {
        PopulateRoomUnits(_visibleUnitsBuffer, TargetRelationship.Any);
        return _visibleUnitsBuffer;
    }

    public List<IUnit> GetVisibleHostileUnitsInScene()
    {
        PopulateRoomUnits(_visibleHostilesBuffer, TargetRelationship.Hostile);
        return _visibleHostilesBuffer;
    }

    public List<Unit> GetHostileUnitsInScene()
    {
        PopulateRoomUnits(_hostileUnitsBuffer, TargetRelationship.Hostile);
        return _hostileUnitsBuffer;
    }

    public List<Unit> GetAlliedUnitsInScene()
    {
        PopulateRoomUnits(_alliedUnitsBuffer, TargetRelationship.Ally);
        return _alliedUnitsBuffer;
    }

    public Unit GetNearestVisibleHostileUnitInScene()
    {
        return GetNearestHostileUnitInScene();
    }

    public Unit GetNearestHostileUnitInScene()
    {
        List<Unit> hostiles = GetHostileUnitsInScene();
        Unit nearest = null;
        float bestSqrDistance = float.MaxValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < hostiles.Count; i++)
        {
            Unit candidate = hostiles[i];
            float sqrDistance = (candidate.Position - Position).sqrMagnitude;

            if (sqrDistance > bestSqrDistance)
                continue;

            if (Mathf.Approximately(sqrDistance, bestSqrDistance) && candidate.CurrentHealth >= bestHealth)
                continue;

            nearest = candidate;
            bestSqrDistance = sqrDistance;
            bestHealth = candidate.CurrentHealth;
        }

        return nearest;
    }

    public Unit GetLowestHealthAllyInScene()
    {
        List<Unit> allies = GetAlliedUnitsInScene();
        Unit lowestHealthAlly = null;
        int lowestHealth = int.MaxValue;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            Unit candidate = allies[i];
            if (candidate.CurrentHealth > lowestHealth)
                continue;

            float sqrDistance = (candidate.Position - Position).sqrMagnitude;
            if (candidate.CurrentHealth == lowestHealth && sqrDistance >= bestSqrDistance)
                continue;

            lowestHealthAlly = candidate;
            lowestHealth = candidate.CurrentHealth;
            bestSqrDistance = sqrDistance;
        }

        return lowestHealthAlly;
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        BattleGrid grid = _roomContext != null
            ? _roomContext.BattleGrid
            : GetComponentInParent<BattleGrid>(includeInactive: true);

        if (grid == null)
            return;

        Vector3Int desiredCell = grid.WorldToCell(transform.position);
        Vector3Int targetCell = grid.IsCellWalkable(desiredCell, this)
            ? desiredCell
            : grid.FindClosestWalkableCell(desiredCell, this);

        transform.position = grid.CellToWorld(targetCell);
    }

    private void PopulateRoomUnits(List<IUnit> results, TargetRelationship relationship)
    {
        IReadOnlyList<Unit> roomUnits = _roomContext != null ? _roomContext.Units : null;
        if (roomUnits == null || results == null)
            return;

        results.Clear();

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!IsValidRoomCandidate(candidate, relationship))
                continue;

            results.Add(candidate);
        }
    }

    private void PopulateRoomUnits(List<Unit> results, TargetRelationship relationship)
    {
        IReadOnlyList<Unit> roomUnits = _roomContext != null ? _roomContext.Units : null;
        if (roomUnits == null || results == null)
            return;

        results.Clear();

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!IsValidRoomCandidate(candidate, relationship))
                continue;

            results.Add(candidate);
        }
    }

    private bool IsValidRoomCandidate(Unit candidate, TargetRelationship relationship)
    {
        if (candidate == null || ReferenceEquals(candidate, this))
            return false;

        if (!candidate.gameObject.activeInHierarchy || !candidate.IsAlive)
            return false;

        if (_roomContext == null || !ReferenceEquals(candidate.RoomContext, _roomContext))
            return false;

        if (!CanDetect(candidate))
            return false;

        return relationship switch
        {
            TargetRelationship.Hostile => IsHostileTo(candidate),
            TargetRelationship.Ally => !IsHostileTo(candidate),
            _ => true
        };
    }

    private IAction ResolveAction()
    {
        if (_actionSource is IAction explicitAction)
            return explicitAction;

        UnitAction attachedAction = GetComponent<UnitAction>();
        if (attachedAction != null)
            return attachedAction;

        return GetComponent<UnitCombat>();
    }
}

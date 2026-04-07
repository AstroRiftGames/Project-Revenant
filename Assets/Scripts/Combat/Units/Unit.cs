using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeController))]
public class Unit : Creature
{
    [SerializeField] private UnitData _unitData;

    private RoomContext _roomContext;
    private readonly List<IUnit> _visibleUnitsBuffer = new();
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

    public void AssignRoomContext(RoomContext context)
    {
        _roomContext = context;
    }

    public List<IUnit> GetVisibleUnitsInScene()
    {
        _visibleUnitsBuffer.Clear();
        PopulateRoomUnits(_visibleUnitsBuffer, includeOnlyHostiles: false);
        return _visibleUnitsBuffer;
    }

    public List<IUnit> GetVisibleHostileUnitsInScene()
    {
        _visibleHostilesBuffer.Clear();
        PopulateRoomUnits(_visibleHostilesBuffer, includeOnlyHostiles: true);
        return _visibleHostilesBuffer;
    }

    public List<Unit> GetHostileUnitsInScene()
    {
        _hostileUnitsBuffer.Clear();

        IReadOnlyList<Unit> roomUnits = _roomContext != null ? _roomContext.Units : null;
        if (roomUnits == null)
            return _hostileUnitsBuffer;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!IsValidRoomCandidate(candidate, includeOnlyHostiles: true))
                continue;

            _hostileUnitsBuffer.Add(candidate);
        }

        return _hostileUnitsBuffer;
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

    private void PopulateRoomUnits(List<IUnit> results, bool includeOnlyHostiles)
    {
        IReadOnlyList<Unit> roomUnits = _roomContext != null ? _roomContext.Units : null;
        if (roomUnits == null || results == null)
            return;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!IsValidRoomCandidate(candidate, includeOnlyHostiles))
                continue;

            results.Add(candidate);
        }
    }

    private bool IsValidRoomCandidate(Unit candidate, bool includeOnlyHostiles)
    {
        if (candidate == null || ReferenceEquals(candidate, this))
            return false;

        if (!candidate.gameObject.activeInHierarchy || !candidate.IsAlive)
            return false;

        if (_roomContext == null || !ReferenceEquals(candidate.RoomContext, _roomContext))
            return false;

        if (!CanDetect(candidate))
            return false;

        if (includeOnlyHostiles && !IsHostileTo(candidate))
            return false;

        return true;
    }
}

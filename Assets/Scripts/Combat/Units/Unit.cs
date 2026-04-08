using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LifeController))]
public class Unit : Creature
    , IGridOccupant
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

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_roomContext != null)
            _roomContext.UnregisterUnit(this);
    }

    public RoomContext RoomContext => _roomContext;
    public IAction Action => _resolvedAction ??= ResolveAction();
    public bool IsDpsMelee => Role == UnitRole.DPS && CombatStyle != UnitCombatStyle.Ranged;
    public bool IsDpsRanged => Role == UnitRole.DPS && CombatStyle == UnitCombatStyle.Ranged;
    public bool WantsToHoldSpacing => Role == UnitRole.Support || IsDpsRanged;
    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => true;

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

    public Unit GetLowestHealthAllyNeedingHelpInScene()
    {
        List<Unit> allies = GetAlliedUnitsInScene();
        Unit bestAlly = null;
        int highestMissingHealth = 0;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            Unit candidate = allies[i];
            int missingHealth = candidate.MaxHealth - candidate.CurrentHealth;
            if (missingHealth <= 0)
                continue;

            float sqrDistance = (candidate.Position - Position).sqrMagnitude;
            if (missingHealth < highestMissingHealth)
                continue;

            if (missingHealth == highestMissingHealth && sqrDistance >= bestSqrDistance)
                continue;

            bestAlly = candidate;
            highestMissingHealth = missingHealth;
            bestSqrDistance = sqrDistance;
        }

        return bestAlly;
    }

    public Unit GetNearestAllyInScene()
    {
        List<Unit> allies = GetAlliedUnitsInScene();
        Unit nearest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            Unit candidate = allies[i];
            float sqrDistance = (candidate.Position - Position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            nearest = candidate;
            bestSqrDistance = sqrDistance;
        }

        return nearest;
    }

    public Unit GetNearestAllyByRoleInScene(UnitRole role)
    {
        List<Unit> allies = GetAlliedUnitsInScene();
        Unit nearest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            Unit candidate = allies[i];
            if (candidate.Role != role)
                continue;

            float sqrDistance = (candidate.Position - Position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            nearest = candidate;
            bestSqrDistance = sqrDistance;
        }

        return nearest;
    }

    public int GetPreferredDistance(IAction action)
    {
        if (action == null)
            return 0;

        return Mathf.Max(0, Mathf.Min(action.PreferredDistanceInCells, action.RangeInCells));
    }

    public Unit GetSpacingThreat(Unit currentTarget)
    {
        if (!WantsToHoldSpacing)
            return null;

        if (Role == UnitRole.Support)
            return GetNearestHostileUnitInScene();

        if (currentTarget != null && IsHostileTo(currentTarget))
            return currentTarget;

        return GetNearestHostileUnitInScene();
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        RoomGrid grid = _roomContext != null
            ? _roomContext.BattleGrid
            : GetComponentInParent<RoomGrid>(includeInactive: true);

        if (grid == null)
            return;

        Vector3Int desiredCell = grid.WorldToCell(transform.position);
        Vector3Int targetCell = grid.IsCellEnterable(desiredCell, this)
            ? desiredCell
            : grid.FindClosestWalkableCell(desiredCell, this);

        transform.position = grid.CellToWorld(targetCell);
    }

    private void PopulateRoomUnits(List<IUnit> results, TargetRelationship relationship)
    {
        if (results == null)
            return;

        results.Clear();
        IReadOnlyList<Unit> roomUnits = _roomContext != null ? _roomContext.Units : null;
        if (roomUnits == null)
            return;

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
        if (results == null)
            return;

        results.Clear();
        IReadOnlyList<Unit> roomUnits = _roomContext != null ? _roomContext.Units : null;
        if (roomUnits == null)
            return;

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

        UnitCombat combat = GetComponent<UnitCombat>();
        if (combat == null)
            return null;

        return Role == UnitRole.Support
            ? new HealCombatAction(this, combat)
            : new AttackCombatAction(this, combat);
    }

    private sealed class AttackCombatAction : IAction
    {
        private readonly UnitCombat _combat;
        private readonly Unit _owner;

        public AttackCombatAction(Unit owner, UnitCombat combat)
        {
            _owner = owner;
            _combat = combat;
        }

        public int RangeInCells => _combat != null ? _combat.AttackRangeInCells : 0;
        public int PreferredDistanceInCells => _owner != null ? Mathf.Max(0, _owner.PreferredDistanceInCells) : RangeInCells;

        public bool IsInRange(Unit self, Unit target)
        {
            return _combat != null && _combat.IsTargetInRange(target);
        }

        public bool CanExecute(Unit self, Unit target)
        {
            if (self == null || target == null || _combat == null)
                return false;

            return self.IsHostileTo(target) && _combat.CanUseOn(target);
        }

        public bool Execute(Unit self, Unit target)
        {
            return CanExecute(self, target) && _combat.TryAttack(target);
        }
    }

    private sealed class HealCombatAction : IAction
    {
        private readonly UnitCombat _combat;
        private readonly Unit _owner;

        public HealCombatAction(Unit owner, UnitCombat combat)
        {
            _owner = owner;
            _combat = combat;
        }

        public int RangeInCells => _combat != null ? _combat.AttackRangeInCells : 0;
        public int PreferredDistanceInCells => _owner != null ? Mathf.Max(0, _owner.PreferredDistanceInCells) : RangeInCells;

        public bool IsInRange(Unit self, Unit target)
        {
            return _combat != null && _combat.IsTargetInRange(target);
        }

        public bool CanExecute(Unit self, Unit target)
        {
            if (self == null || target == null || _combat == null)
                return false;

            if (self.IsHostileTo(target))
                return false;

            if (target.CurrentHealth >= target.MaxHealth)
                return false;

            return _combat.CanUseOn(target);
        }

        public bool Execute(Unit self, Unit target)
        {
            if (!CanExecute(self, target))
                return false;

            return _combat.TryExecute(target, candidate => candidate.Heal(self.AttackDamage, self));
        }
    }
}

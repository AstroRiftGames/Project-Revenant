using System.Collections.Generic;
using UnityEngine;

public enum UnitRole { Tank, DPS, Support }
public enum UnitFaction { Goblin, Skeleton, Human, Animal, Golem }

public abstract class Creature : MonoBehaviour, IUnit
{
    [SerializeField] private bool _useObstacleDetection = true;
    [SerializeField] private LayerMask _obstacleMask;

    public string Id { get; private set; }
    public UnitRole Role => _data.role;
    public UnitFaction Faction => _data.faction;
    public float VisionRange => _data.stats.visionRange;
    public float MoveSpeed => _data.stats.moveSpeed;
    public int AttackDamage => _data.stats.attackDamage;
    public float AttackInterval => _data.stats.attackInterval;
    public int AttackRangeInCells => _data.stats.attackRangeInCells;
    public float Accuracy => _data.stats.accuracy;
    public float Evasion => _data.stats.evasion;
    public Vector3 Position => transform.position;
    public int CurrentHealth => _lifeController != null ? _lifeController.CurrentHealth : 0;
    public int MaxHealth => _lifeController != null ? _lifeController.MaxHealth : BaseMaxHealth;
    public bool IsAlive => _lifeController != null && _lifeController.IsAlive;
    public int BaseMaxHealth => _data != null ? _data.stats.maxHealth : 0;

    protected UnitData _data;
    protected LifeController _lifeController;

    protected virtual void Awake()
    {
        _lifeController = GetComponent<LifeController>();
    }

    protected virtual void Initialize(UnitData data)
    {
        if (data.stats == null)
            data.stats = new UnitStatsData();

        _data = data;
        Id = data.unitId;
        _lifeController ??= GetComponent<LifeController>();
        _lifeController?.Initialize(data.stats.maxHealth);
    }

    public bool IsHostileTo(IUnit candidate)
    {
        if (candidate == null)
            return false;

        if (ReferenceEquals(candidate, this))
            return false;

        if (!candidate.IsAlive)
            return false;

        return Faction != candidate.Faction;
    }

    public bool CanDetect(IUnit candidate)
    {
        if (candidate == null)
            return false;

        if (ReferenceEquals(candidate, this))
            return false;

        if (_data == null)
            return false;

        if (!IsAlive)
            return false;

        if (!candidate.IsAlive)
            return false;

        bool inRange = IsWithinVisionRange(candidate);
        bool blocked = IsDetectionBlocked(candidate);
        return inRange && !blocked;
    }

    public void GetVisibleUnits(List<IUnit> candidates, List<IUnit> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            IUnit candidate = candidates[i];
            if (CanDetect(candidate))
                results.Add(candidate);
        }
    }

    public void GetVisibleHostileUnits(List<IUnit> candidates, List<IUnit> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            IUnit candidate = candidates[i];
            if (candidate != null && IsHostileTo(candidate) && CanDetect(candidate))
                results.Add(candidate);
        }
    }

    public IUnit GetNearestVisibleHostileUnit(List<IUnit> candidates, List<IUnit> visibleHostilesBuffer)
    {
        GetVisibleHostileUnits(candidates, visibleHostilesBuffer);
        IUnit nearest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < visibleHostilesBuffer.Count; i++)
        {
            float distance = Vector3.Distance(Position, visibleHostilesBuffer[i].Position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = visibleHostilesBuffer[i];
        }

        return nearest;
    }

    protected virtual bool IsWithinVisionRange(IUnit candidate)
        => Vector3.Distance(Position, candidate.Position) <= VisionRange;

    protected virtual bool IsDetectionBlocked(IUnit candidate)
    {
        if (!_useObstacleDetection)
            return false;

        Vector2 origin = Position;
        Vector2 target = candidate.Position;
        RaycastHit2D[] hits = Physics2D.LinecastAll(origin, target, _obstacleMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            if (hitCollider.transform == transform)
                continue;

            return true;
        }

        return false;
    }

    public void TakeDamage(int amount, IUnit source = null)
    {
        _lifeController?.TakeDamage(amount, source);
    }

    public Unit GetLastAttacker()
    {
        return _lifeController != null ? _lifeController.LastAttacker : null;
    }

    public List<Unit> GetAliveAggressors()
    {
        return _lifeController != null ? _lifeController.GetAliveAggressors() : new List<Unit>();
    }

}

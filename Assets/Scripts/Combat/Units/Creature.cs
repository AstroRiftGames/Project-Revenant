using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum UnitRole { Tank, DPS, Support }
public enum UnitFaction { Goblin, Skeleton, Human, Animal, Golem }

public abstract class Creature : MonoBehaviour, IUnit, IDamageable
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
    public Vector3 Position => transform.position;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => _data != null ? _data.stats.maxHealth : 0;
    public bool IsAlive => CurrentHealth > 0;

    protected UnitData _data;

    protected virtual void Initialize(UnitData data)
    {
        if (data.stats == null)
            data.stats = new UnitStatsData();

        _data = data;
        Id = data.unitId;
        CurrentHealth = data.stats.maxHealth;
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

    public List<IUnit> GetVisibleUnits(IEnumerable<IUnit> candidates)
    {
        if (candidates == null)
            return new List<IUnit>();

        return candidates.Where(CanDetect).ToList();
    }

    public List<IUnit> GetVisibleHostileUnits(IEnumerable<IUnit> candidates)
    {
        if (candidates == null)
            return new List<IUnit>();

        return candidates.Where(candidate => IsHostileTo(candidate) && CanDetect(candidate)).ToList();
    }

    public IUnit GetNearestVisibleHostileUnit(IEnumerable<IUnit> candidates)
    {
        List<IUnit> visibleHostiles = GetVisibleHostileUnits(candidates);
        IUnit nearest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < visibleHostiles.Count; i++)
        {
            float distance = Vector3.Distance(Position, visibleHostiles[i].Position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = visibleHostiles[i];
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

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (CurrentHealth == 0)
            Die();
    }

    protected virtual void Die()
    {
        gameObject.SetActive(false);
    }

}

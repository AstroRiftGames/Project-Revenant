using System.Collections.Generic;
using System.Linq;
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
    public float VisionRange => _data.visionRange;
    public float VisionAngle => _data.visionAngle;
    public Vector3 Position => transform.position;

    protected UnitData _data;

    protected virtual void Initialize(UnitData data)
    {
        _data = data;
        Id = data.unitId;
    }

    public bool CanDetect(IUnit candidate)
    {
        if (candidate == null)
            return false;

        if (ReferenceEquals(candidate, this))
            return false;

        if (_data == null)
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

}

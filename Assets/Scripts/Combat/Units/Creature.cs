using System.Collections.Generic;
using UnityEngine;

public enum UnitRole { Tank, DPS, Support }
public enum UnitFaction { Goblin, Skeleton, Human, Animal, Golem }
public enum UnitAttackKind { Melee, Projectile, SupportProjectile }

public abstract class Creature : MonoBehaviour, IUnit
{
    public string Id { get; protected set; } = string.Empty;
    public UnitTeam Team => _data != null ? _data.team : UnitTeam.Enemy;
    public UnitRole Role => _data != null ? _data.role : default;
    public UnitCombatStyle CombatStyle => _data != null ? _data.combatStyle : UnitCombatStyle.Default;
    public UnitAttackKind AttackPresentation => ResolveAttackPresentation();
    public UnitFaction Faction => _data != null ? _data.faction : default;
    public Vector3 Position => transform.position;
    public int CurrentHealth => _lifeController != null ? _lifeController.CurrentHealth : 0;
    public int MaxHealth => _lifeController != null ? _lifeController.MaxHealth : 0;
    public bool IsAlive => _lifeController == null || _lifeController.IsAlive;
    public int BaseMaxHealth => _data != null && _data.stats != null ? _data.stats.maxHealth : 0;
    public float MoveSpeed => _data != null && _data.stats != null ? _data.stats.moveSpeed : 0f;
    public int AttackRangeInCells => _data != null && _data.stats != null ? _data.stats.attackRangeInCells : 0;
    public int PreferredDistanceInCells => _data != null && _data.stats != null ? _data.stats.preferredDistanceInCells : 0;
    public int AttackDamage => _data != null && _data.stats != null ? _data.stats.attackDamage : 0;
    public float AttackInterval => _data != null && _data.stats != null ? _data.stats.attackInterval : 0f;
    public float Accuracy => _data != null && _data.stats != null ? _data.stats.accuracy : 0f;
    public float Evasion => _data != null && _data.stats != null ? _data.stats.evasion : 0f;

    protected UnitData _data;
    protected LifeController _lifeController { get; private set; }

    protected virtual void Awake()
    {
        _lifeController = GetComponent<LifeController>();
    }

    protected virtual void Initialize(UnitData data)
    {
        _data = data;
        Id = data != null ? data.unitId : string.Empty;
        _lifeController ??= GetComponent<LifeController>();

        if (_lifeController != null)
            _lifeController.Initialize(BaseMaxHealth);
    }

    public bool IsHostileTo(IUnit candidate)
    {
        if (candidate == null || ReferenceEquals(candidate, this))
            return false;

        if (candidate is not Creature creature)
            return false;

        if (!IsAlive || !creature.IsAlive)
            return false;

        return Team != creature.Team;
    }

    public bool CanDetect(IUnit candidate)
    {
        if (candidate == null || ReferenceEquals(candidate, this))
            return false;

        if (candidate is not MonoBehaviour behaviour)
            return false;

        return behaviour.gameObject.activeInHierarchy;
    }

    public void GetVisibleUnits(List<IUnit> candidates, List<IUnit> results)
    {
        results?.Clear();
    }

    public void GetVisibleHostileUnits(List<IUnit> candidates, List<IUnit> results)
    {
        results?.Clear();
    }

    public IUnit GetNearestVisibleHostileUnit(List<IUnit> candidates, List<IUnit> visibleHostilesBuffer)
    {
        if (visibleHostilesBuffer == null)
            return null;

        GetVisibleHostileUnits(candidates, visibleHostilesBuffer);
        IUnit nearest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < visibleHostilesBuffer.Count; i++)
        {
            IUnit candidate = visibleHostilesBuffer[i];
            float sqrDistance = (candidate.Position - Position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearest = candidate;
        }

        return nearest;
    }

    public void TakeDamage(int amount, IUnit source = null)
    {
        _lifeController?.TakeDamage(amount, source);
    }

    public void Heal(int amount, IUnit source = null)
    {
        _lifeController?.Heal(amount, source);
    }

    public Unit GetLastAttacker()
    {
        return _lifeController != null ? _lifeController.LastAttacker : null;
    }

    public List<Unit> GetAliveAggressors()
    {
        return _lifeController != null ? _lifeController.GetAliveAggressors() : new List<Unit>();
    }

    private UnitAttackKind ResolveAttackPresentation()
    {
        if (Role == UnitRole.Support)
            return UnitAttackKind.SupportProjectile;

        if (Role == UnitRole.DPS && CombatStyle == UnitCombatStyle.Ranged)
            return UnitAttackKind.Projectile;

        return UnitAttackKind.Melee;
    }
}

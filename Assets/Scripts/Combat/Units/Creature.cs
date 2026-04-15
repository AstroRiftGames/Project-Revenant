using System;
using Selection.Core;
using Selection.Interfaces;
using System.Collections.Generic;
using UnityEngine;

public enum UnitRole { Tank, DPS, Support }
public enum UnitFaction { Goblin, Skeleton, Human, Animal, Golem }
public enum UnitAttackKind { Melee, Projectile, SupportProjectile }

[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(UnitAffiliationState))]
public abstract class Creature : MonoBehaviour, IUnit, ISelectable, ICharacterStatsProvider
{
    public static event Action<Creature> OnCreatureEnabled;
    public static event Action<Creature> OnCreatureAffiliationChanged;
    public string Id { get; protected set; } = string.Empty;
    public UnitTeam Team => _affiliationState.Team;
    public UnitRole Role => _data != null ? _data.role : default;
    public UnitCombatStyle CombatStyle => _data != null ? _data.combatStyle : UnitCombatStyle.Default;
    public UnitTargetingMode TargetingMode => _data != null ? _data.targetingMode : UnitTargetingMode.RolePriority;
    public UnitAttackKind AttackPresentation => ResolveAttackPresentation();
    public UnitFaction Faction => _affiliationState.Faction;
    public Vector3 Position => transform.position;
    public bool IsEnemy => Team == UnitTeam.Enemy;
    public bool IsAlly => Team == UnitTeam.NecromancerAlly;
    public UnitLifecycleState LifecycleState => _recruitableState.CurrentState;
    public bool IsRecruitable => _recruitableState.IsRecruitable;

    public int CurrentHealth => _lifeController != null ? _lifeController.CurrentHealth : 0;
    public int MaxHealth => _lifeController != null ? _lifeController.MaxHealth : 0;
    public bool IsAlive => _lifeController == null || _lifeController.IsAlive;
    public int BaseMaxHealth => _data != null && _data.stats != null ? _data.stats.maxHealth : 0;
    public float MoveSpeed => _data != null && _data.stats != null ? _data.stats.moveSpeed : 0f;
    public int AttackRangeInCells => _data != null && _data.stats != null ? _data.stats.attackRangeInCells : 0;
    public int PreferredDistanceInCells => _data != null && _data.stats != null ? _data.stats.preferredDistanceInCells : 0;
    public int AttackDamage => _data != null && _data.stats != null ? _data.stats.attackDamage : 0;
    public float AttackCooldown => _data != null && _data.stats != null ? _data.stats.attackCooldown : 0f;
    public float Accuracy => _data != null && _data.stats != null ? _data.stats.accuracy : 0f;
    public float Evasion => _data != null && _data.stats != null ? _data.stats.evasion : 0f;

    protected UnitData _data;
    protected LifeController _lifeController { get; private set; }
    private RecruitableUnitState _recruitableState;
    private UnitAffiliationState _affiliationState;


    [Header("Selection Visuals")]
    [SerializeField] private GameObject selectionIndicator;
    public float CurrentAbilityCooldown => 10f; //TODO: This should come from UnitData when ability system is implemented
    public float MaxAbilityCooldown => 10; //TODO: This should come from UnitData when ability system is implemented
    public Sprite AbilityIcon => null;  //TODO: This should come from UnitData when ability system is implemented
    public Sprite CharacterSprite => _data.sprite;
    public bool IsSelected { get; private set; }
    public GameObject SelectionGameObject => gameObject;
    public ICharacterStatsProvider StatsProvider => this;
    

    protected virtual void Awake()
    {
        _lifeController = GetComponent<LifeController>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _affiliationState = GetComponent<UnitAffiliationState>();
        ValidateRequiredComponents();
    }

    protected virtual void OnEnable()
    {
        OnCreatureEnabled?.Invoke(this);
    }

    protected virtual void Initialize(UnitData data)
    {
        _data = data;
        Id = data != null ? data.unitId : string.Empty;
        _lifeController ??= GetComponent<LifeController>();
        _recruitableState ??= GetComponent<RecruitableUnitState>();
        _affiliationState ??= GetComponent<UnitAffiliationState>();
        _affiliationState.Initialize(data);

        if (_lifeController != null)
            _lifeController.Initialize(BaseMaxHealth);
    }

    public void SetAffiliation(UnitTeam team, UnitFaction faction)
    {
        _affiliationState.SetAffiliation(team, faction);
        OnCreatureAffiliationChanged?.Invoke(this);
    }

    public void ResetAffiliationFromData()
    {
        _affiliationState.Initialize(_data);
    }

    public UnitData GetUnitData()
    {
        return _data;
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

    private void ValidateRequiredComponents()
    {
        if (_lifeController == null || _recruitableState == null || _affiliationState == null)
            throw new InvalidOperationException($"[{nameof(Creature)}] Missing required runtime components on '{name}'.");
    }

    public event System.Action<ISelectable> OnSelectionInvalidated;

    protected virtual void OnDisable()
    {
        if (IsSelected)
        {
            OnSelectionInvalidated?.Invoke(this);
        }
    }

    public void Select()
    {
        IsSelected = true;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    public void Deselect()
    {
        IsSelected = false;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }
}

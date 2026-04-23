using System;
using Selection.Core;
using Selection.Interfaces;
using System.Collections.Generic;
using UnityEngine;

public enum UnitRole { Tank, DPS, Support }
public enum UnitFaction { None, Human, Orc, Reptilian, Insectoid, Golems, Igneous, Aquatic, WildBeast }
public enum UnitAttackKind { Melee, Projectile, SupportProjectile }

[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(UnitAffiliationState))]
[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(StatusEffectVisualFeedback))]
[RequireComponent(typeof(StatusEffectDebugPopupPresenter))]
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
    public float MoveSpeed => ResolveFinalFloatStat(CombatStatType.MoveSpeed, _data != null && _data.stats != null ? _data.stats.moveSpeed : 0f);
    public int AttackRangeInCells => ResolveFinalIntStat(CombatStatType.Range, _data != null && _data.stats != null ? _data.stats.attackRangeInCells : 0);
    public int PreferredDistanceInCells => _data != null && _data.stats != null ? _data.stats.preferredDistanceInCells : 0;
    public int AttackDamage => ResolveFinalIntStat(CombatStatType.Damage, _data != null && _data.stats != null ? _data.stats.attackDamage : 0);
    public float AttackCooldown => _data != null && _data.stats != null ? _data.stats.attackCooldown : 0f;
    public float Accuracy => ResolveFinalFloatStat(CombatStatType.Accuracy, _data != null && _data.stats != null ? _data.stats.accuracy : 0f);
    public float Evasion => _data != null && _data.stats != null ? _data.stats.evasion : 0f;
    public int Defense => ResolveFinalIntStat(CombatStatType.Defense, _data != null && _data.stats != null ? _data.stats.defense : 0);
    public StatusEffectController StatusEffects => _statusEffectController;

    protected UnitData _data;
    protected LifeController _lifeController { get; private set; }
    private RecruitableUnitState _recruitableState;
    private UnitAffiliationState _affiliationState;
    private SkillCaster _skillCaster;
    private StatusEffectController _statusEffectController;


    [Header("Selection Visuals")]
    [SerializeField] private GameObject selectionIndicator;
    public float CurrentAbilityCooldown => _skillCaster != null ? _skillCaster.CurrentCooldown : 0f;
    public float MaxAbilityCooldown => _skillCaster != null ? _skillCaster.MaxCooldown : 0f;
    public Sprite AbilityIcon => _skillCaster != null ? _skillCaster.Icon : null;
    public Sprite CharacterSprite => _data != null ? _data.sprite : null;
    public bool IsSelected { get; private set; }
    public GameObject SelectionGameObject => gameObject;
    public ICharacterStatsProvider StatsProvider => this;
    public UnitStatsData CoreStats => _data != null ? _data.stats : null;
    

    protected virtual void Awake()
    {
        _lifeController = GetComponent<LifeController>();
        _recruitableState = GetComponent<RecruitableUnitState>();
        _affiliationState = GetComponent<UnitAffiliationState>();
        _skillCaster = GetComponent<SkillCaster>();
        _statusEffectController = GetComponent<StatusEffectController>();
        if (_statusEffectController == null)
        {
            Debug.LogWarning(
                $"[{nameof(Creature)}] '{name}' was missing {nameof(StatusEffectController)} at runtime and it was auto-added. " +
                "Update the prefab setup to include it explicitly.",
                this);
            _statusEffectController = gameObject.AddComponent<StatusEffectController>();
        }

        if (GetComponent<StatusEffectVisualFeedback>() == null)
        {
            Debug.LogWarning(
                $"[{nameof(Creature)}] '{name}' was missing {nameof(StatusEffectVisualFeedback)} at runtime and it was auto-added. " +
                "Update the prefab setup to include it explicitly if this unit should show status feedback.",
                this);
            gameObject.AddComponent<StatusEffectVisualFeedback>();
        }

        if (GetComponent<StatusEffectDebugPopupPresenter>() == null)
        {
            Debug.LogWarning(
                $"[{nameof(Creature)}] '{name}' was missing {nameof(StatusEffectDebugPopupPresenter)} at runtime and it was auto-added. " +
                "Update the prefab setup to include it explicitly while persistent status debug is enabled.",
                this);
            gameObject.AddComponent<StatusEffectDebugPopupPresenter>();
        }

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
        _lifeController?.TakeDamage(ResolveIncomingDamage(amount, source), source);
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
        if (_lifeController == null || _recruitableState == null || _affiliationState == null || _statusEffectController == null)
            throw new InvalidOperationException($"[{nameof(Creature)}] Missing required runtime components on '{name}'.");
    }

    private float ResolveFinalFloatStat(CombatStatType statType, float baseValue)
    {
        if (_statusEffectController == null)
            return baseValue;

        float additiveModifier = _statusEffectController.GetModifierTotal(statType, StatusModifierOperation.Additive);
        float multiplierModifier = _statusEffectController.GetModifierTotal(statType, StatusModifierOperation.Multiplier);
        return Mathf.Max(0f, (baseValue + additiveModifier) * (1f + multiplierModifier));
    }

    private int ResolveFinalIntStat(CombatStatType statType, int baseValue)
    {
        return Mathf.RoundToInt(ResolveFinalFloatStat(statType, baseValue));
    }

    private int ResolveIncomingDamage(int rawDamage, IUnit source)
    {
        if (rawDamage <= 0)
            return 0;

        int mitigatedDamage = rawDamage - Defense;
        return Mathf.Max(0, mitigatedDamage);
    }

    public event System.Action<ISelectable> OnSelectionInvalidated;
    public event System.Action<ISelectable, bool> OnSelectionStateChanged;

    protected virtual void OnDisable()
    {
        if (IsSelected)
        {
            ApplySelectionState(false);
            OnSelectionInvalidated?.Invoke(this);
        }
    }

    public void Select()
    {
        ApplySelectionState(true);
    }

    public void Deselect()
    {
        ApplySelectionState(false);
    }

    private void ApplySelectionState(bool isSelected)
    {
        if (IsSelected == isSelected)
            return;

        IsSelected = isSelected;
        if (selectionIndicator != null)
            selectionIndicator.SetActive(isSelected);

        OnSelectionStateChanged?.Invoke(this, isSelected);
    }
}

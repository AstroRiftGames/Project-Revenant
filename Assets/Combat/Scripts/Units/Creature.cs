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
[RequireComponent(typeof(UnitVisualMaterialController))]
[RequireComponent(typeof(StatusEffectVisualFeedback))]
[RequireComponent(typeof(UnitSelectionFeedbackView))]
public abstract class Creature : MonoBehaviour, IUnit, ISelectable, ICharacterStatsProvider
{
    public static event Action<Creature> OnCreatureEnabled;
    public static event Action<Creature> OnCreatureAffiliationChanged;
    public string Id { get; protected set; } = string.Empty;
    public string DisplayName => _data != null ? _data.displayName : string.Empty;
    public UnitTeam Team => _affiliationState.Team;
    public UnitRole Role => _data != null ? _data.role : default;
    public UnitCombatStyle CombatStyle => _data != null ? _data.combatStyle : UnitCombatStyle.Default;
    public UnitTargetingMode TargetingMode => _data != null ? _data.targetingMode : UnitTargetingMode.RolePriority;
    public UnitAttackKind AttackPresentation => ResolveAttackPresentation();
    public UnitFaction Faction => _affiliationState.Faction;
    public Vector3 Position => transform.position;
    public bool IsEnemy => Team == UnitTeam.Enemy;
    public bool IsAlly => Team == UnitTeam.Ally;
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
    private UnitSelectionFeedbackView _selectionFeedbackView;


    [Header("Selection Visuals")]
    [SerializeField] private GameObject selectionIndicator;
    public float CurrentAbilityCharge => _skillCaster != null ? _skillCaster.CurrentCharge : 0f;
    public float MaxAbilityCharge => _skillCaster != null ? _skillCaster.MaxCharge : 0f;
    public bool IsAbilityReady => _skillCaster != null && _skillCaster.IsSkillReady;
    public bool UsesAbilityChargeVisual => _skillCaster != null && _skillCaster.UsesAbilityChargeVisual;
    public Sprite AbilityIcon => _skillCaster != null ? _skillCaster.Icon : null;
    public SkillData Skill => _data != null ? _data.skill : null;
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
        _statusEffectController = ResolveStatusEffectController();

        ResolveVisualMaterialController();
        ResolveStatusEffectVisualFeedback();

        _selectionFeedbackView = ResolveSelectionFeedbackView();
        _selectionFeedbackView?.Configure(selectionIndicator);
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

        if (LifecycleState != UnitLifecycleState.Alive || creature.LifecycleState != UnitLifecycleState.Alive)
            return false;

        return Team != creature.Team;
    }

    public bool CanDetect(IUnit candidate)
    {
        if (candidate == null || ReferenceEquals(candidate, this))
            return false;

        if (candidate is not Creature creature)
            return false;

        if (!creature.IsAlive || creature.LifecycleState != UnitLifecycleState.Alive)
            return false;

        return creature.gameObject.activeInHierarchy;
    }

    public void TakeDamage(int amount, IUnit source = null)
    {
        int resolvedDamage = ResolveIncomingDamage(amount, source);
        if (resolvedDamage > 0)
            _statusEffectController?.HandleIncomingAttack();

        _lifeController?.TakeDamage(resolvedDamage, source);
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

    private StatusEffectController ResolveStatusEffectController()
    {
        if (TryGetComponent(out StatusEffectController statusEffectController))
            return statusEffectController;

        Debug.LogError(
            $"[{nameof(Creature)}] Missing required authoring component '{nameof(StatusEffectController)}' on '{name}'. " +
            $"Object path: '{BuildHierarchyPath(transform)}'. Scene: '{ResolveScenePath()}'. " +
            "Prefab is misconfigured; add it to the prefab root.",
            this);
        return null;
    }

    private UnitVisualMaterialController ResolveVisualMaterialController()
    {
        if (TryGetComponent(out UnitVisualMaterialController visualMaterialController))
            return visualMaterialController;

        Debug.LogWarning(
            $"[{nameof(Creature)}] Missing visual authoring component '{nameof(UnitVisualMaterialController)}' on '{name}'. " +
            $"Object path: '{BuildHierarchyPath(transform)}'. Scene: '{ResolveScenePath()}'. " +
            "Visual feedback may degrade. Add it to the prefab root manually.",
            this);
        return null;
    }

    private StatusEffectVisualFeedback ResolveStatusEffectVisualFeedback()
    {
        if (TryGetComponent(out StatusEffectVisualFeedback statusEffectVisualFeedback))
            return statusEffectVisualFeedback;

        Debug.LogWarning(
            $"[{nameof(Creature)}] Missing visual authoring component '{nameof(StatusEffectVisualFeedback)}' on '{name}'. " +
            $"Object path: '{BuildHierarchyPath(transform)}'. Scene: '{ResolveScenePath()}'. " +
            "Status/corpse/recruit visual feedback may degrade. Add it to the prefab root manually.",
            this);
        return null;
    }

    private UnitSelectionFeedbackView ResolveSelectionFeedbackView()
    {
        UnitSelectionFeedbackView feedbackView = GetComponent<UnitSelectionFeedbackView>();
        if (feedbackView != null)
            return feedbackView;

        Debug.LogError(
            $"[{nameof(Creature)}] Missing required authoring component '{nameof(UnitSelectionFeedbackView)}' on '{name}'. " +
            $"Object path: '{BuildHierarchyPath(transform)}'. Scene: '{ResolveScenePath()}'. " +
            "Add it to the prefab root; the legacy runtime fallback was removed.",
            this);
        return null;
    }

    private string ResolveScenePath()
    {
        var scene = gameObject.scene;
        if (!scene.IsValid())
            return "<invalid scene>";

        if (!string.IsNullOrWhiteSpace(scene.path))
            return scene.path;

        return !string.IsNullOrWhiteSpace(scene.name) ? scene.name : "<runtime scene>";
    }

    private static string BuildHierarchyPath(Transform target)
    {
        if (target == null)
            return "<missing transform>";

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
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
        if (_selectionFeedbackView != null)
        {
            if (isSelected)
                _selectionFeedbackView.ShowSelected();
            else
                _selectionFeedbackView.HideSelected();
        }

        OnSelectionStateChanged?.Invoke(this, isSelected);
    }
}

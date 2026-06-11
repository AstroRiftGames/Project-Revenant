using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(UnitVisualMaterialController))]
public class StatusEffectVisualFeedback : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private StatusEffectController _statusEffectController;
    private LifeController _lifeController;
    private RecruitableUnitState _recruitableUnitState;
    private UnitDeathHandler _unitDeathHandler;
    private UnitVisualMaterialController _unitVisualMaterialController;
    private UnitVisualMaterialState _currentVisualState = (UnitVisualMaterialState)(-1);
    private bool _hasLoggedMissingVisualMaterialController;

    private void Awake()
    {
        ResolveReferences();
        ForceRefreshVisualState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (_statusEffectController != null)
        {
            _statusEffectController.EffectApplied += HandleEffectApplied;
            _statusEffectController.EffectRefreshed += HandleEffectChanged;
            _statusEffectController.EffectStackChanged += HandleEffectChanged;
            _statusEffectController.EffectRemoved += HandleEffectRemoved;
        }

        if (_recruitableUnitState != null)
            _recruitableUnitState.OnStateChanged += HandleLifecycleStateChanged;

        ForceRefreshVisualState();
    }

    private void OnDisable()
    {
        if (_statusEffectController != null)
        {
            _statusEffectController.EffectApplied -= HandleEffectApplied;
            _statusEffectController.EffectRefreshed -= HandleEffectChanged;
            _statusEffectController.EffectStackChanged -= HandleEffectChanged;
            _statusEffectController.EffectRemoved -= HandleEffectRemoved;
        }

        if (_recruitableUnitState != null)
            _recruitableUnitState.OnStateChanged -= HandleLifecycleStateChanged;

        ApplyResolvedState(UnitVisualMaterialState.Normal);
    }

    private void HandleEffectApplied(StatusEffectController controller, ActiveStatusEffect activeEffect)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        ForceRefreshVisualState();
    }

    private void HandleEffectChanged(StatusEffectController controller, ActiveStatusEffect activeEffect)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        ForceRefreshVisualState();
    }

    private void HandleEffectRemoved(StatusEffectController controller, ActiveStatusEffect activeEffect, StatusEffectRemovalReason removalReason)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        ForceRefreshVisualState();
    }

    private void HandleLifecycleStateChanged(UnitLifecycleState state)
    {
        ForceRefreshVisualState();
    }

    private UnitVisualMaterialState ResolveCurrentVisualState()
    {
        if (_unitDeathHandler != null && _unitDeathHandler.IsSoulAbsorbedCorpse)
            return UnitVisualMaterialState.SoulAbsorbed;

        if (_recruitableUnitState != null && _recruitableUnitState.IsRecruitable)
            return UnitVisualMaterialState.Recruitable;

        if (_lifeController != null && !_lifeController.IsAlive)
            return UnitVisualMaterialState.Dead;

        return ResolveStatusVisualState();
    }

    private UnitVisualMaterialState ResolveStatusVisualState()
    {
        if (_statusEffectController == null)
            return UnitVisualMaterialState.Normal;

        IReadOnlyList<ActiveStatusEffect> activeEffects = _statusEffectController.ActiveEffects;
        UnitVisualMaterialState bestState = UnitVisualMaterialState.Normal;
        int bestPriority = int.MinValue;
        List<string> debugEntries = _debugLogs ? new List<string>(activeEffects.Count) : null;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = activeEffects[i];
            StatusEffectDefinition definition = activeEffect != null ? activeEffect.Definition : null;
            if (definition == null)
                continue;

            StatusVisualStyle visualStyle = ResolveRepresentableVisualStyle(definition);
            UnitVisualMaterialState candidateState = MapVisualStyleToState(visualStyle);
            int priority = ResolveVisualPriority(candidateState);
            debugEntries?.Add($"{definition.DisplayName}:{visualStyle}->{candidateState}");
            if (priority <= bestPriority)
                continue;

            bestPriority = priority;
            bestState = candidateState;
        }

        if (_debugLogs && debugEntries != null && debugEntries.Count > 0)
            LogDebug($"[StatusEffectVisualFeedback] '{name}' active status visuals: {string.Join(", ", debugEntries)}. Winner={bestState}.");

        return bestState;
    }

    private void ResolveReferences()
    {
        _statusEffectController ??= GetComponent<StatusEffectController>();
        _lifeController ??= GetComponent<LifeController>();
        _recruitableUnitState ??= GetComponent<RecruitableUnitState>();
        _unitDeathHandler ??= GetComponent<UnitDeathHandler>();
        _unitVisualMaterialController ??= ResolveVisualMaterialController();
    }

    private UnitVisualMaterialController ResolveVisualMaterialController()
    {
        if (TryGetComponent(out UnitVisualMaterialController visualMaterialController))
            return visualMaterialController;

        if (_unitVisualMaterialController == null)
            LogMissingVisualMaterialController();

        return null;
    }

    private void LogMissingVisualMaterialController()
    {
        if (_hasLoggedMissingVisualMaterialController)
            return;

        Debug.LogWarning(
            $"[{nameof(StatusEffectVisualFeedback)}] Missing visual authoring component '{nameof(UnitVisualMaterialController)}' on '{name}'. " +
            $"Object path: '{BuildHierarchyPath(transform)}'. Scene: '{ResolveScenePath()}'. " +
            "Status and lifecycle visual feedback may degrade. Add it to the prefab root manually.",
            this);
        _hasLoggedMissingVisualMaterialController = true;
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

    private void ForceRefreshVisualState()
    {
        ApplyResolvedState(ResolveCurrentVisualState());
    }

    private void ApplyResolvedState(UnitVisualMaterialState visualState)
    {
        if (_currentVisualState == visualState)
            return;

        _currentVisualState = visualState;
        _unitVisualMaterialController?.SetBaseState(visualState);

        LogDebug($"[StatusEffectVisualFeedback] '{name}' applied visual state={visualState}.");
    }

    private static UnitVisualMaterialState MapVisualStyleToState(StatusVisualStyle style)
    {
        return style switch
        {
            StatusVisualStyle.Stun => UnitVisualMaterialState.Stun,
            StatusVisualStyle.DamageOverTimePermanent => UnitVisualMaterialState.DamageOverTimePermanent,
            StatusVisualStyle.DamageOverTime => UnitVisualMaterialState.DamageOverTime,
            StatusVisualStyle.Debuff => UnitVisualMaterialState.Debuff,
            StatusVisualStyle.HealOverTime => UnitVisualMaterialState.HealOverTime,
            StatusVisualStyle.Buff => UnitVisualMaterialState.Buff,
            _ => UnitVisualMaterialState.Normal
        };
    }

    private static StatusVisualStyle ResolveRepresentableVisualStyle(StatusEffectDefinition definition)
    {
        if (definition == null)
            return StatusVisualStyle.None;

        if (definition.VisualStyle != StatusVisualStyle.None)
            return definition.VisualStyle;

return definition.EffectType switch
        {
            SkillEffectKind.Stun => StatusVisualStyle.Stun,
            SkillEffectKind.Heal => StatusVisualStyle.HealOverTime,
            SkillEffectKind.PoisonBurn when definition.DurationMode == StatusEffectDurationMode.PermanentUntilDeath => StatusVisualStyle.DamageOverTimePermanent,
            SkillEffectKind.PoisonBurn => StatusVisualStyle.DamageOverTime,
            SkillEffectKind.StrengthBuff => StatusVisualStyle.Buff,
            SkillEffectKind.Haste => StatusVisualStyle.Buff,
            SkillEffectKind.Slow => StatusVisualStyle.Debuff,
            SkillEffectKind.Knockback => StatusVisualStyle.Knockback,
            _ => StatusVisualStyle.None
        };
    }

    private static int ResolveVisualPriority(UnitVisualMaterialState state)
    {
        return state switch
        {
            UnitVisualMaterialState.Dead => 1000,
            UnitVisualMaterialState.SoulAbsorbed => 900,
            UnitVisualMaterialState.Recruitable => 800,
            UnitVisualMaterialState.Stun => 600,
            UnitVisualMaterialState.DamageOverTimePermanent => 500,
            UnitVisualMaterialState.DamageOverTime => 400,
            UnitVisualMaterialState.Debuff => 300,
            UnitVisualMaterialState.HealOverTime => 200,
            UnitVisualMaterialState.Buff => 100,
            _ => 0
        };
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

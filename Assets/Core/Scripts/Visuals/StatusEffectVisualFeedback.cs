using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
[RequireComponent(typeof(UnitVisualMaterialController))]
public class StatusEffectVisualFeedback : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private SkillTextPopup _popupPrefab;
    [SerializeField] private Vector3 _popupOffset = new(0f, 0.95f, 0f);

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private StatusEffectController _statusEffectController;
    private LifeController _lifeController;
    private RecruitableUnitState _recruitableUnitState;
    private UnitDeathHandler _unitDeathHandler;
    private UnitVisualMaterialController _unitVisualMaterialController;
    private UnitVisualMaterialState _currentVisualState = (UnitVisualMaterialState)(-1);

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
        ShowPopup(ResolveApplyPopupText(activeEffect), ResolveEffectColor(activeEffect));
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

        if (removalReason == StatusEffectRemovalReason.Expired &&
            activeEffect != null &&
            activeEffect.Definition != null &&
            activeEffect.Definition.ShowExpirePopup)
        {
            ShowPopup(activeEffect.Definition.ExpirePopupText, ResolveEffectColor(activeEffect));
        }

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
        _unitVisualMaterialController ??= GetComponent<UnitVisualMaterialController>() ?? gameObject.AddComponent<UnitVisualMaterialController>();
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
            StatusEffectType.Stun => StatusVisualStyle.Stun,
            StatusEffectType.Sleep => StatusVisualStyle.Stun,
            StatusEffectType.Silence => StatusVisualStyle.Debuff,
            StatusEffectType.Fear => StatusVisualStyle.Debuff,
            StatusEffectType.Taunt => StatusVisualStyle.Debuff,
            StatusEffectType.Heal => StatusVisualStyle.HealOverTime,
            StatusEffectType.HealOverTime => StatusVisualStyle.HealOverTime,
            StatusEffectType.DamageOverTime when definition.DurationMode == StatusEffectDurationMode.PermanentUntilDeath => StatusVisualStyle.DamageOverTimePermanent,
            StatusEffectType.DamageOverTime => StatusVisualStyle.DamageOverTime,
            StatusEffectType.StatModifierBuff => StatusVisualStyle.Buff,
            StatusEffectType.StatModifierDebuff => StatusVisualStyle.Debuff,
            StatusEffectType.Invisibility => StatusVisualStyle.Invisible,
            StatusEffectType.Invincibility => StatusVisualStyle.Invincible,
            StatusEffectType.Incorruptible => StatusVisualStyle.Incorruptible,
            StatusEffectType.Berserk => StatusVisualStyle.Buff,
            StatusEffectType.LifeSteal => StatusVisualStyle.LifeSteal,
            StatusEffectType.Knockback => StatusVisualStyle.Knockback,
            _ => StatusVisualStyle.None
        };
    }

private static string ResolveApplyPopupText(ActiveStatusEffect activeEffect)
    {
        StatusEffectDefinition definition = activeEffect != null ? activeEffect.Definition : null;
        if (definition == null)
            return null;

        string popupText = definition.ApplyPopupText;
        if (string.IsNullOrWhiteSpace(popupText))
            popupText = definition.DisplayName;

        string result = popupText;

        int stacks = activeEffect.StackCount;
        if (stacks > 1)
            result += $" x{stacks}";

        if (definition.HasTimedDuration && activeEffect.ExpiresAt > 0)
        {
            float remaining = activeEffect.ExpiresAt - Time.time;
            if (remaining > 0)
                result += $" ({remaining:0.0}s)";
        }

        return result;
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

    private Color ResolveEffectColor(ActiveStatusEffect activeEffect)
    {
        StatusVisualStyle style = activeEffect != null && activeEffect.Definition != null
            ? ResolveRepresentableVisualStyle(activeEffect.Definition)
            : StatusVisualStyle.None;

        return ResolvePopupColor(MapVisualStyleToState(style));
    }

    private void ShowPopup(string message, Color color)
    {
        if (string.IsNullOrWhiteSpace(message) || _popupPrefab == null)
            return;

        SkillTextPopup popup = Instantiate(_popupPrefab, transform);
        popup.name = $"Status Popup - {message}";
        popup.transform.localPosition = _popupOffset;
        popup.transform.localRotation = Quaternion.identity;
        popup.Initialize(message, color);
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }

    private static Color ResolvePopupColor(UnitVisualMaterialState state)
    {
        return state switch
        {
            UnitVisualMaterialState.Stun => new Color(1f, 0.9f, 0.25f, 1f),
            UnitVisualMaterialState.DamageOverTimePermanent => new Color(0.62f, 0.2f, 0.9f, 1f),
            UnitVisualMaterialState.DamageOverTime => new Color(1f, 0.38f, 0.38f, 1f),
            UnitVisualMaterialState.Debuff => new Color(0.95f, 0.45f, 0.78f, 1f),
            UnitVisualMaterialState.HealOverTime => new Color(0.35f, 1f, 0.55f, 1f),
            UnitVisualMaterialState.Buff => new Color(0.35f, 0.85f, 1f, 1f),
            _ => Color.white
        };
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(LifeController))]
[RequireComponent(typeof(RecruitableUnitState))]
public class StatusEffectVisualFeedback : MonoBehaviour
{
    private enum UnitVisualState
    {
        Normal,
        Buff,
        HealOverTime,
        Debuff,
        DamageOverTime,
        DamageOverTimePermanent,
        Stun,
        Dead,
        Recruitable
    }

    [Serializable]
    private struct VisualStateColorBinding
    {
        [SerializeField] private UnitVisualState _state;
        [SerializeField] private Color _color;

        public VisualStateColorBinding(UnitVisualState state, Color color)
        {
            _state = state;
            _color = color;
        }

        public UnitVisualState State => _state;
        public Color Color => _color;
    }

    [Header("Popup")]
    [SerializeField] private SkillTextPopup _popupPrefab;
    [SerializeField] private Vector3 _popupOffset = new(0f, 0.95f, 0f);

    [Header("Colors")]
    [SerializeField] private VisualStateColorBinding[] _stateColors =
    {
        new(UnitVisualState.Normal, Color.white),
        new(UnitVisualState.Buff, new Color(0.35f, 0.85f, 1f, 1f)),
        new(UnitVisualState.HealOverTime, new Color(0.35f, 1f, 0.55f, 1f)),
        new(UnitVisualState.Debuff, new Color(0.95f, 0.45f, 0.78f, 1f)),
        new(UnitVisualState.DamageOverTime, new Color(1f, 0.38f, 0.38f, 1f)),
        new(UnitVisualState.DamageOverTimePermanent, new Color(0.62f, 0.2f, 0.9f, 1f)),
        new(UnitVisualState.Stun, new Color(1f, 0.9f, 0.25f, 1f)),
        new(UnitVisualState.Dead, new Color(0.239f, 0.239f, 0.239f, 1f)),
        new(UnitVisualState.Recruitable, Color.black),
    };

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private StatusEffectController _statusEffectController;
    private LifeController _lifeController;
    private RecruitableUnitState _recruitableUnitState;
    private SpriteRenderer[] _spriteRenderers;
    private UnitVisualState _currentVisualState = UnitVisualState.Normal;
    private Color _currentResolvedColor = Color.white;

    private void Awake()
    {
        ResolveReferences();
        RefreshRendererCache();
        ForceRefreshVisualState();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshRendererCache();

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

        ApplyResolvedColor(UnitVisualState.Normal, ResolveColor(UnitVisualState.Normal));
    }

    private void Update()
    {
        RefreshVisualState();
    }

    private void HandleEffectApplied(StatusEffectController controller, ActiveStatusEffect activeEffect)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        ForceRefreshVisualState();
        ShowPopup(activeEffect != null ? activeEffect.Definition?.ApplyPopupText : null, ResolveEffectColor(activeEffect));
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

    private UnitVisualState ResolveCurrentVisualState()
    {
        if (_recruitableUnitState != null && _recruitableUnitState.IsRecruitable)
            return UnitVisualState.Recruitable;

        if (_lifeController != null && !_lifeController.IsAlive)
            return UnitVisualState.Dead;

        return ResolveStatusVisualState();
    }

    private UnitVisualState ResolveStatusVisualState()
    {
        if (_statusEffectController == null)
            return UnitVisualState.Normal;

        IReadOnlyList<ActiveStatusEffect> activeEffects = _statusEffectController.ActiveEffects;
        UnitVisualState bestState = UnitVisualState.Normal;
        int bestPriority = int.MinValue;
        List<string> debugEntries = _debugLogs ? new List<string>(activeEffects.Count) : null;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = activeEffects[i];
            StatusEffectDefinition definition = activeEffect != null ? activeEffect.Definition : null;
            if (definition == null)
                continue;

            StatusVisualStyle visualStyle = ResolveRepresentableVisualStyle(definition);
            UnitVisualState candidateState = MapVisualStyleToState(visualStyle);
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
    }

    private void RefreshRendererCache()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        LogDebug(
            $"[StatusEffectVisualFeedback] '{name}' refs: status={_statusEffectController != null}, life={_lifeController != null}, " +
            $"recruitable={_recruitableUnitState != null}, renderers={(_spriteRenderers != null ? _spriteRenderers.Length : 0)}.");
    }

    private void RefreshVisualState()
    {
        UnitVisualState resolvedState = ResolveCurrentVisualState();
        Color resolvedColor = ResolveColor(resolvedState);

        if (_currentVisualState == resolvedState && ColorsEqual(_currentResolvedColor, resolvedColor))
            return;

        ApplyResolvedColor(resolvedState, resolvedColor);
    }

    private void ForceRefreshVisualState()
    {
        ApplyResolvedColor(ResolveCurrentVisualState(), ResolveColor(ResolveCurrentVisualState()));
    }

    private void ApplyResolvedColor(UnitVisualState visualState, Color resolvedColor)
    {
        _currentVisualState = visualState;
        _currentResolvedColor = resolvedColor;

        if (_spriteRenderers == null)
            return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            Color color = resolvedColor;
            color.a = spriteRenderer.color.a;
            spriteRenderer.color = color;
        }

        LogDebug(
            $"[StatusEffectVisualFeedback] '{name}' applied state={visualState} color=({resolvedColor.r:F2}, {resolvedColor.g:F2}, {resolvedColor.b:F2}, {resolvedColor.a:F2}).");
    }

    private Color ResolveColor(UnitVisualState state)
    {
        if (_stateColors != null)
        {
            for (int i = 0; i < _stateColors.Length; i++)
            {
                if (_stateColors[i].State == state)
                    return _stateColors[i].Color;
            }
        }

        return Color.white;
    }

    private static UnitVisualState MapVisualStyleToState(StatusVisualStyle style)
    {
        return style switch
        {
            StatusVisualStyle.Stun => UnitVisualState.Stun,
            StatusVisualStyle.DamageOverTimePermanent => UnitVisualState.DamageOverTimePermanent,
            StatusVisualStyle.DamageOverTime => UnitVisualState.DamageOverTime,
            StatusVisualStyle.Debuff => UnitVisualState.Debuff,
            StatusVisualStyle.HealOverTime => UnitVisualState.HealOverTime,
            StatusVisualStyle.Buff => UnitVisualState.Buff,
            _ => UnitVisualState.Normal
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
            StatusEffectType.HealOverTime => StatusVisualStyle.HealOverTime,
            StatusEffectType.DamageOverTime when definition.DurationMode == StatusEffectDurationMode.PermanentUntilDeath => StatusVisualStyle.DamageOverTimePermanent,
            StatusEffectType.DamageOverTime => StatusVisualStyle.DamageOverTime,
            StatusEffectType.StatModifier when definition.StatModifier.Value < 0f => StatusVisualStyle.Debuff,
            StatusEffectType.StatModifier when definition.StatModifier.Value > 0f => StatusVisualStyle.Buff,
            _ => StatusVisualStyle.None
        };
    }

    private static int ResolveVisualPriority(UnitVisualState state)
    {
        return state switch
        {
            UnitVisualState.Recruitable => 800,
            UnitVisualState.Dead => 700,
            UnitVisualState.Stun => 600,
            UnitVisualState.DamageOverTimePermanent => 500,
            UnitVisualState.DamageOverTime => 400,
            UnitVisualState.Debuff => 300,
            UnitVisualState.HealOverTime => 200,
            UnitVisualState.Buff => 100,
            _ => 0
        };
    }

    private Color ResolveEffectColor(ActiveStatusEffect activeEffect)
    {
        StatusVisualStyle style = activeEffect != null && activeEffect.Definition != null
            ? ResolveRepresentableVisualStyle(activeEffect.Definition)
            : StatusVisualStyle.None;

        return ResolveColor(MapVisualStyleToState(style));
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

    private static bool ColorsEqual(Color left, Color right)
    {
        const float epsilon = 0.001f;
        return Mathf.Abs(left.r - right.r) <= epsilon &&
               Mathf.Abs(left.g - right.g) <= epsilon &&
               Mathf.Abs(left.b - right.b) <= epsilon &&
               Mathf.Abs(left.a - right.a) <= epsilon;
    }
}

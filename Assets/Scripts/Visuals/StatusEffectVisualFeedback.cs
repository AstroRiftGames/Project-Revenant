using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
public class StatusEffectVisualFeedback : MonoBehaviour
{
    [Serializable]
    private struct VisualStyleColorBinding
    {
        [SerializeField] private StatusVisualStyle _style;
        [SerializeField] private Color _color;

        public VisualStyleColorBinding(StatusVisualStyle style, Color color)
        {
            _style = style;
            _color = color;
        }

        public StatusVisualStyle Style => _style;
        public Color Color => _color;
    }

    [Header("Popup")]
    [SerializeField] private SkillTextPopup _popupPrefab;
    [SerializeField] private Vector3 _popupOffset = new(0f, 0.95f, 0f);

    [Header("Tint")]
    [SerializeField] private float _tintBlend = 0.35f;
    [SerializeField] private float _tintLerpSpeed = 10f;
    [SerializeField] private VisualStyleColorBinding[] _styleColors =
    {
        new(StatusVisualStyle.Stun, new Color(1f, 0.9f, 0.25f, 1f)),
        new(StatusVisualStyle.DamageOverTimePermanent, new Color(0.62f, 0.2f, 0.9f, 1f)),
        new(StatusVisualStyle.DamageOverTime, new Color(1f, 0.38f, 0.38f, 1f)),
        new(StatusVisualStyle.Debuff, new Color(0.95f, 0.45f, 0.78f, 1f)),
        new(StatusVisualStyle.HealOverTime, new Color(0.35f, 1f, 0.55f, 1f)),
        new(StatusVisualStyle.Buff, new Color(0.35f, 0.85f, 1f, 1f)),
    };

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private StatusEffectController _statusEffectController;
    private SpriteRenderer[] _spriteRenderers;
    private Color[] _baseColors;
    private Color _currentTintColor = Color.white;
    private Color _targetTintColor = Color.white;

    private void Awake()
    {
        _statusEffectController = GetComponent<StatusEffectController>();
        CacheSpriteRenderers();
        _currentTintColor = Color.white;
        _targetTintColor = Color.white;
        ApplyTintImmediately(_currentTintColor);
    }

    private void OnEnable()
    {
        if (_statusEffectController == null)
            return;

        _statusEffectController.EffectApplied += HandleEffectApplied;
        _statusEffectController.EffectRefreshed += HandleEffectChanged;
        _statusEffectController.EffectStackChanged += HandleEffectChanged;
        _statusEffectController.EffectRemoved += HandleEffectRemoved;
        RefreshVisualState();
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

        RestoreBaseColors();
    }

    private void Update()
    {
        if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            return;

        if (_currentTintColor == _targetTintColor)
            return;

        _currentTintColor = Color.Lerp(_currentTintColor, _targetTintColor, 1f - Mathf.Exp(-_tintLerpSpeed * Time.deltaTime));
        if (AreColorsClose(_currentTintColor, _targetTintColor))
            _currentTintColor = _targetTintColor;

        ApplyTintImmediately(_currentTintColor);
    }

    private void HandleEffectApplied(StatusEffectController controller, ActiveStatusEffect activeEffect)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        RefreshVisualState();
        ShowPopup(activeEffect != null ? activeEffect.Definition?.ApplyPopupText : null, ResolveEffectColor(activeEffect));
    }

    private void HandleEffectChanged(StatusEffectController controller, ActiveStatusEffect activeEffect)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        RefreshVisualState();
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

        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        StatusVisualStyle dominantStyle = ResolveDominantVisualStyle();
        _targetTintColor = ResolveTintColor(dominantStyle);
        LogDebug($"[StatusEffectVisualFeedback] '{name}' dominant style: {dominantStyle}.");
    }

    private StatusVisualStyle ResolveDominantVisualStyle()
    {
        if (_statusEffectController == null)
            return StatusVisualStyle.None;

        IReadOnlyList<ActiveStatusEffect> activeEffects = _statusEffectController.ActiveEffects;
        StatusVisualStyle bestStyle = StatusVisualStyle.None;
        int bestPriority = int.MinValue;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = activeEffects[i];
            StatusEffectDefinition definition = activeEffect != null ? activeEffect.Definition : null;
            if (definition == null)
                continue;

            StatusVisualStyle style = definition.VisualStyle;
            int priority = ResolveVisualPriority(style);
            if (priority <= bestPriority)
                continue;

            bestPriority = priority;
            bestStyle = style;
        }

        return bestStyle;
    }

    private int ResolveVisualPriority(StatusVisualStyle style)
    {
        return style switch
        {
            StatusVisualStyle.Stun => 600,
            StatusVisualStyle.DamageOverTimePermanent => 500,
            StatusVisualStyle.DamageOverTime => 400,
            StatusVisualStyle.Debuff => 300,
            StatusVisualStyle.HealOverTime => 200,
            StatusVisualStyle.Buff => 100,
            _ => 0
        };
    }

    private Color ResolveTintColor(StatusVisualStyle style)
    {
        if (style == StatusVisualStyle.None)
            return Color.white;

        if (_styleColors != null)
        {
            for (int i = 0; i < _styleColors.Length; i++)
            {
                if (_styleColors[i].Style == style)
                    return _styleColors[i].Color;
            }
        }

        return Color.white;
    }

    private void CacheSpriteRenderers()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        _baseColors = new Color[_spriteRenderers.Length];

        for (int i = 0; i < _spriteRenderers.Length; i++)
            _baseColors[i] = _spriteRenderers[i] != null ? _spriteRenderers[i].color : Color.white;
    }

    private void RestoreBaseColors()
    {
        if (_spriteRenderers == null || _baseColors == null)
            return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            spriteRenderer.color = _baseColors[i];
        }
    }

    private void ApplyTintImmediately(Color tintColor)
    {
        if (_spriteRenderers == null || _baseColors == null)
            return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            Color baseColor = _baseColors[i];
            Color blendedColor = Color.Lerp(baseColor, MultiplyColor(baseColor, tintColor), _tintBlend);
            blendedColor.a = baseColor.a;
            spriteRenderer.color = blendedColor;
        }
    }

    private Color ResolveEffectColor(ActiveStatusEffect activeEffect)
    {
        StatusVisualStyle style = activeEffect != null && activeEffect.Definition != null
            ? activeEffect.Definition.VisualStyle
            : StatusVisualStyle.None;

        Color resolvedColor = ResolveTintColor(style);
        return resolvedColor == Color.white ? Color.white : resolvedColor;
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

    private static Color MultiplyColor(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
    }

    private static bool AreColorsClose(Color left, Color right)
    {
        const float epsilon = 0.005f;
        return Mathf.Abs(left.r - right.r) <= epsilon &&
               Mathf.Abs(left.g - right.g) <= epsilon &&
               Mathf.Abs(left.b - right.b) <= epsilon &&
               Mathf.Abs(left.a - right.a) <= epsilon;
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

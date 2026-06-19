using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Data;
using Selection.UI;

public class UnitHealthBarEffectFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _effectsContainer;
    [SerializeField] private EffectIcon _effectIconPrefab;
    [SerializeField] private GameIconDatabase _iconDatabase;

    [Header("Settings")]
    [SerializeField] private int _maxVisibleEffects = 3;

    private StatusEffectController _statusEffectController;
    private readonly List<EffectIcon> _activeIcons = new List<EffectIcon>();

    private void Awake()
    {
        if (_effectsContainer == null)
            Debug.LogWarning($"[{nameof(UnitHealthBarEffectFeedback)}] EffectsContainer not assigned on {gameObject.name}.", this);

        if (_effectIconPrefab == null)
            Debug.LogWarning($"[{nameof(UnitHealthBarEffectFeedback)}] EffectIconPrefab not assigned on {gameObject.name}.", this);

        if (_iconDatabase == null)
            Debug.LogWarning($"[{nameof(UnitHealthBarEffectFeedback)}] GameIconDatabase not assigned on {gameObject.name}.", this);
    }

    public void Initialize(StatusEffectController statusEffectController)
    {
        if (statusEffectController == null)
            return;

        if (_statusEffectController != null)
        {
            UnsubscribeFromController();
        }

        _statusEffectController = statusEffectController;
        SubscribeToController();
        RefreshEffects();
    }

    private void SubscribeToController()
    {
        if (_statusEffectController == null) return;

        _statusEffectController.EffectApplied += OnEffectApplied;
        _statusEffectController.EffectRefreshed += OnEffectRefreshed;
        _statusEffectController.EffectStackChanged += OnEffectStackChanged;
        _statusEffectController.EffectRemoved += OnEffectRemoved;
    }

    private void UnsubscribeFromController()
    {
        if (_statusEffectController == null) return;

        _statusEffectController.EffectApplied -= OnEffectApplied;
        _statusEffectController.EffectRefreshed -= OnEffectRefreshed;
        _statusEffectController.EffectStackChanged -= OnEffectStackChanged;
        _statusEffectController.EffectRemoved -= OnEffectRemoved;
    }

    private void OnEffectApplied(StatusEffectController controller, ActiveStatusEffect effect)
    {
        RefreshEffects();
    }

    private void OnEffectRefreshed(StatusEffectController controller, ActiveStatusEffect effect)
    {
        RefreshEffects();
    }

    private void OnEffectStackChanged(StatusEffectController controller, ActiveStatusEffect effect)
    {
        RefreshEffects();
    }

    private void OnEffectRemoved(StatusEffectController controller, ActiveStatusEffect effect, StatusEffectRemovalReason reason)
    {
        RefreshEffects();
    }

    private void RefreshEffects()
    {
        ClearIcons();

        if (_statusEffectController == null || _iconDatabase == null)
            return;

        var activeEffects = _statusEffectController.ActiveEffects;
        if (activeEffects == null || activeEffects.Count == 0)
            return;

        if (_effectIconPrefab == null || _effectsContainer == null)
            return;

        int visibleCount = 0;
        for (int i = 0; i < activeEffects.Count && visibleCount < _maxVisibleEffects; i++)
        {
            var effect = activeEffects[i];
            if (effect == null || effect.Definition == null) continue;

            var (sprite, color) = _iconDatabase.GetEffectIcon(effect.Definition.EffectType);
            if (sprite == null) continue;

            EffectIcon icon = Instantiate(_effectIconPrefab, _effectsContainer);
            icon.Initialize(sprite, color, effect.Definition.DurationSeconds);
            _activeIcons.Add(icon);
            visibleCount++;
        }
    }

    private void ClearIcons()
    {
        foreach (var icon in _activeIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        _activeIcons.Clear();
    }

    private void OnDisable()
    {
        UnsubscribeFromController();
        ClearIcons();
    }

    private void OnDestroy()
    {
        UnsubscribeFromController();
        ClearIcons();
    }
}

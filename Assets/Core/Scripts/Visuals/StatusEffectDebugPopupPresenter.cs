using System;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(StatusEffectController))]
public class StatusEffectDebugPopupPresenter : MonoBehaviour
{
    private const string DefaultPopupPrefabPath = "UI/StatusEffectPersistentPopup";

    [Header("Popup")]
    [SerializeField] private StatusEffectPersistentPopup _popupPrefab;
    [SerializeField] private Vector3 _worldOffset = new(0f, 1.35f, 0f);

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private readonly StringBuilder _builder = new();
    private StatusEffectController _statusEffectController;
    private StatusEffectPersistentPopup _activePopup;

    private void Awake()
    {
        _statusEffectController = GetComponent<StatusEffectController>();
        _popupPrefab ??= Resources.Load<StatusEffectPersistentPopup>(DefaultPopupPrefabPath);
    }

    private void OnEnable()
    {
        if (_statusEffectController == null)
            return;

        _statusEffectController.EffectApplied += HandleEffectChanged;
        _statusEffectController.EffectRefreshed += HandleEffectChanged;
        _statusEffectController.EffectStackChanged += HandleEffectChanged;
        _statusEffectController.EffectRemoved += HandleEffectRemoved;
        RefreshPopup();
    }

    private void OnDisable()
    {
        if (_statusEffectController != null)
        {
            _statusEffectController.EffectApplied -= HandleEffectChanged;
            _statusEffectController.EffectRefreshed -= HandleEffectChanged;
            _statusEffectController.EffectStackChanged -= HandleEffectChanged;
            _statusEffectController.EffectRemoved -= HandleEffectRemoved;
        }

        DestroyPopup();
    }

    private void Update()
    {
        if (_statusEffectController == null || _statusEffectController.ActiveEffects.Count == 0 || _activePopup == null)
            return;

        RefreshPopup();
    }

    private void HandleEffectChanged(StatusEffectController controller, ActiveStatusEffect activeEffect)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        RefreshPopup();
    }

    private void HandleEffectRemoved(StatusEffectController controller, ActiveStatusEffect activeEffect, StatusEffectRemovalReason removalReason)
    {
        if (!ReferenceEquals(controller, _statusEffectController))
            return;

        RefreshPopup();
    }

    private void RefreshPopup()
    {
        if (_statusEffectController == null)
            return;

        string content = BuildContent(Time.time);
        if (string.IsNullOrWhiteSpace(content))
        {
            DestroyPopup();
            return;
        }

        StatusEffectPersistentPopup popup = EnsurePopup();
        if (popup == null)
            return;

        popup.SetText(content);
        popup.SetVisible(true);
        LogDebug($"[StatusEffectDebugPopupPresenter] '{name}' refreshed popup: {content.Replace(Environment.NewLine, " | ")}");
    }

    private string BuildContent(float now)
    {
        _builder.Clear();

        for (int i = 0; i < _statusEffectController.ActiveEffects.Count; i++)
        {
            ActiveStatusEffect activeEffect = _statusEffectController.ActiveEffects[i];
            if (activeEffect == null || activeEffect.Definition == null)
                continue;

            if (_builder.Length > 0)
                _builder.AppendLine();

            AppendEffectLine(activeEffect, now);
        }

        return _builder.ToString();
    }

    private void AppendEffectLine(ActiveStatusEffect activeEffect, float now)
    {
        StatusEffectDefinition definition = activeEffect.Definition;
        string shortName = !string.IsNullOrWhiteSpace(definition.ApplyPopupText)
            ? definition.ApplyPopupText
            : definition.DisplayName;

        _builder.Append(shortName);

        if (activeEffect.StackCount > 1 || definition.DurationMode == StatusEffectDurationMode.PermanentUntilDeath)
        {
            _builder.Append(" x");
            _builder.Append(activeEffect.StackCount);
        }

        if (definition.HasTimedDuration)
        {
            float remainingSeconds = Mathf.Max(0f, activeEffect.ExpiresAt - now);
            _builder.Append(' ');
            _builder.Append(remainingSeconds.ToString("0.0"));
            _builder.Append('s');
        }
    }

    private StatusEffectPersistentPopup EnsurePopup()
    {
        if (_activePopup != null)
            return _activePopup;

        if (_popupPrefab == null)
        {
            Debug.LogWarning(
                $"[{nameof(StatusEffectDebugPopupPresenter)}] '{name}' could not resolve popup prefab at Resources/{DefaultPopupPrefabPath}.",
                this);
            return null;
        }

        Canvas targetCanvas = ResolveTargetCanvas();
        if (targetCanvas == null)
        {
            Debug.LogWarning(
                $"[{nameof(StatusEffectDebugPopupPresenter)}] '{name}' could not resolve a screen-space canvas for persistent status debug.",
                this);
            return null;
        }

        _activePopup = Instantiate(_popupPrefab, targetCanvas.transform);
        _activePopup.name = $"Status Debug Popup - {name}";
        _activePopup.Initialize(targetCanvas, transform, _worldOffset);
        return _activePopup;
    }

    private void DestroyPopup()
    {
        if (_activePopup == null)
            return;

        Destroy(_activePopup.gameObject);
        _activePopup = null;
    }

    private static Canvas ResolveTargetCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isRootCanvas)
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                return canvas;
        }

        return null;
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

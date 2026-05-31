using System.Collections.Generic;
using UnityEngine;

public enum UnitVisualMaterialState
{
    Normal,
    Buff,
    HealOverTime,
    Debuff,
    DamageOverTime,
    DamageOverTimePermanent,
    Stun,
    Dead,
    Recruitable,
    SoulAbsorbed
}

[DisallowMultipleComponent]
public class UnitVisualMaterialController : MonoBehaviour
{
    private const string RuntimeOverlayObjectSuffix = "_VisualFeedbackOverlay";

    private sealed class RendererBinding
    {
        public SpriteRenderer SourceRenderer;
        public SpriteRenderer OverlayRenderer;
        public bool RuntimeOverlay;
    }

    [System.Serializable]
    private struct VisualStateBinding
    {
        [SerializeField] private UnitVisualMaterialState _state;
        [SerializeField] private Color _overlayColor;
        [SerializeField, Range(0f, 1f)] private float _overlayAmount;

        public VisualStateBinding(UnitVisualMaterialState state, Color overlayColor, float overlayAmount)
        {
            _state = state;
            _overlayColor = overlayColor;
            _overlayAmount = overlayAmount;
        }

        public UnitVisualMaterialState State => _state;
        public Color OverlayColor => _overlayColor;
        public float OverlayAmount => _overlayAmount;
    }

    [Header("Material")]
    [SerializeField] private Material _unitVisualMaterial;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer _sourceRenderer;
    [SerializeField] private SpriteRenderer _overlayRenderer;
    [SerializeField] private SpriteRenderer[] _sourceRenderers;
    [SerializeField] private SpriteRenderer[] _overlayRenderers;
    [SerializeField] [Min(0)] private int _overlaySortingOrderOffset = 1;

    [Header("Blink")]
    [SerializeField] private Color _blinkColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float _blinkAmount = 1f;
    [SerializeField] private string _flashColorPropertyName = "_FlashColor";
    [SerializeField] private string _flashAmountPropertyName = "_FlashAmount";
    [SerializeField] private string _overlayColorPropertyName = "_OverlayColor";
    [SerializeField] private string _overlayAmountPropertyName = "_OverlayAmount";

    [Header("State Bindings")]
    [SerializeField] private VisualStateBinding[] _stateBindings =
    {
        new VisualStateBinding(),
    };

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private MaterialPropertyBlock _propertyBlock;
    private int _flashColorPropertyId;
    private int _flashAmountPropertyId;
    private int _overlayColorPropertyId;
    private int _overlayAmountPropertyId;
    private UnitVisualMaterialState _baseState;
    private bool _blinkOverrideActive;
    private readonly List<RendererBinding> _rendererBindings = new();
    private bool _hasLoggedMissingOverlayWarning;
    private bool _hasLoggedMissingMaterialWarning;
    private bool _hasLoggedResolvedRenderers;

    private void Reset()
    {
        EnsureDefaultBindings(forceReset: true);
        AutoAssignSafeRendererReferences();
    }

    private void Awake()
    {
        EnsureDefaultBindings();
        EnsureRuntimeState();

        ResolveRendererReferences();
        EnsureOverlayMaterial();
        ApplyVisualState();
    }

    private void OnEnable()
    {
        EnsureDefaultBindings();
        EnsureRuntimeState();
        ResolveRendererReferences();
        EnsureOverlayMaterial();
        ApplyVisualState();
    }

    private void OnDisable()
    {
        _blinkOverrideActive = false;
        DisableOverlay();
    }

    private void OnDestroy()
    {
        DestroyRuntimeOverlayRenderers();
    }

    private void OnValidate()
    {
        EnsureDefaultBindings();
        _overlaySortingOrderOffset = Mathf.Max(0, _overlaySortingOrderOffset);

        if (!Application.isPlaying)
            AutoAssignSafeRendererReferences();
    }

    public void SetBaseState(UnitVisualMaterialState state)
    {
        if (_baseState == state)
            return;

        _baseState = state;
        ApplyVisualState();
    }

    public void SetBlinkOverride(bool isActive)
    {
        if (_blinkOverrideActive == isActive)
            return;

        _blinkOverrideActive = isActive;
        ApplyVisualState();
    }

    public void RefreshRenderers()
    {
        ResolveRendererReferences();
        EnsureOverlayMaterial();
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        EnsureRuntimeState();
        ResolveRendererReferences();
        if (!CanRenderOverlay())
        {
            DisableOverlay();
            return;
        }
        SyncOverlayRenderers();

        VisualStateBinding binding = ResolveBinding(_baseState);
        bool shouldRenderOverlay = _blinkOverrideActive || binding.OverlayAmount > 0f;

        if (!shouldRenderOverlay)
        {
            DisableOverlay(clearPropertyBlock: true);
            LogDebug($"[UnitVisualMaterialController] '{name}' applied state={_baseState}, blink={_blinkOverrideActive}, overlay=off.");
            return;
        }

        EnsureOverlayMaterial();
        if (_unitVisualMaterial == null)
        {
            DisableOverlay(clearPropertyBlock: true);
            return;
        }

        for (int i = 0; i < _rendererBindings.Count; i++)
            ApplyOverlayProperties(_rendererBindings[i].OverlayRenderer, binding);

        LogDebug($"[UnitVisualMaterialController] '{name}' applied state={_baseState}, blink={_blinkOverrideActive}, overlay=on.");
    }

    private void ApplyOverlayProperties(SpriteRenderer overlayRenderer, VisualStateBinding binding)
    {
        if (overlayRenderer == null)
            return;

        overlayRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.Clear();
        _propertyBlock.SetColor(_overlayColorPropertyId, binding.OverlayColor);
        _propertyBlock.SetFloat(_overlayAmountPropertyId, binding.OverlayAmount);
        _propertyBlock.SetColor(_flashColorPropertyId, _blinkColor);
        _propertyBlock.SetFloat(_flashAmountPropertyId, _blinkOverrideActive ? _blinkAmount : 0f);
        overlayRenderer.SetPropertyBlock(_propertyBlock);
        overlayRenderer.enabled = true;
    }

    private VisualStateBinding ResolveBinding(UnitVisualMaterialState state)
    {
        if (_stateBindings != null)
        {
            for (int i = 0; i < _stateBindings.Length; i++)
            {
                if (_stateBindings[i].State == state)
                    return _stateBindings[i];
            }
        }

        return CreateBinding(UnitVisualMaterialState.Normal, Color.clear, 0f);
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }

    private void ResolveRendererReferences()
    {
        if (_sourceRenderer == null)
            _sourceRenderer = GetComponent<SpriteRenderer>();

        if (_overlayRenderer == null)
            _overlayRenderer = ResolveOverlayRenderer();

        RebuildRendererBindings();

        if (_debugLogs && !_hasLoggedResolvedRenderers)
        {
            _hasLoggedResolvedRenderers = true;
            Debug.Log($"[UnitVisualMaterialController] '{name}' renderer bindings: {FormatRendererBindingsForDebug()}.", this);
        }
    }

    private void AutoAssignSafeRendererReferences()
    {
        _sourceRenderer ??= GetComponent<SpriteRenderer>();
        if (_sourceRenderer != null)
            _overlayRenderer ??= ResolveOverlayRenderer();
    }

    private SpriteRenderer ResolveOverlayRenderer()
    {
        Transform visualOverlay = transform.Find("VisualOverlay");
        if (visualOverlay != null)
        {
            SpriteRenderer childOverlay = visualOverlay.GetComponent<SpriteRenderer>();
            if (childOverlay != null && !ReferenceEquals(childOverlay, _sourceRenderer))
                return childOverlay;

            childOverlay = visualOverlay.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
            if (childOverlay != null && !ReferenceEquals(childOverlay, _sourceRenderer))
                return childOverlay;
        }

        return null;
    }

    private void RebuildRendererBindings()
    {
        _rendererBindings.Clear();

        if (HasExplicitSourceRendererCollection())
        {
            for (int i = 0; i < _sourceRenderers.Length; i++)
            {
                SpriteRenderer sourceRenderer = _sourceRenderers[i];
                SpriteRenderer overlayRenderer = ResolveOverlayRendererForSource(i, sourceRenderer, createRuntimeOverlay: true);
                AddRendererBinding(sourceRenderer, overlayRenderer, overlayRenderer != null && IsRuntimeOverlayRenderer(sourceRenderer, overlayRenderer));
            }

            return;
        }

        AddRendererBinding(_sourceRenderer, _overlayRenderer, runtimeOverlay: false);
    }

    private bool HasExplicitSourceRendererCollection()
    {
        if (_sourceRenderers == null)
            return false;

        for (int i = 0; i < _sourceRenderers.Length; i++)
        {
            if (_sourceRenderers[i] != null)
                return true;
        }

        return false;
    }

    private SpriteRenderer ResolveOverlayRendererForSource(int index, SpriteRenderer sourceRenderer, bool createRuntimeOverlay)
    {
        if (sourceRenderer == null)
            return null;

        if (_overlayRenderers != null && index >= 0 && index < _overlayRenderers.Length)
        {
            SpriteRenderer configuredOverlay = _overlayRenderers[index];
            if (configuredOverlay != null && !ReferenceEquals(configuredOverlay, sourceRenderer))
                return configuredOverlay;
        }

        if (ReferenceEquals(sourceRenderer, _sourceRenderer) && _overlayRenderer != null && !ReferenceEquals(_overlayRenderer, sourceRenderer))
            return _overlayRenderer;

        if (!createRuntimeOverlay)
            return null;

        return ResolveExistingRuntimeOverlayRenderer(sourceRenderer) ?? CreateRuntimeOverlayRenderer(sourceRenderer);
    }

    private void AddRendererBinding(SpriteRenderer sourceRenderer, SpriteRenderer overlayRenderer, bool runtimeOverlay)
    {
        if (sourceRenderer == null || overlayRenderer == null || ReferenceEquals(sourceRenderer, overlayRenderer))
            return;

        _rendererBindings.Add(new RendererBinding
        {
            SourceRenderer = sourceRenderer,
            OverlayRenderer = overlayRenderer,
            RuntimeOverlay = runtimeOverlay
        });
    }

    private SpriteRenderer ResolveExistingRuntimeOverlayRenderer(SpriteRenderer sourceRenderer)
    {
        if (sourceRenderer == null)
            return null;

        Transform overlayTransform = sourceRenderer.transform.Find($"{sourceRenderer.name}{RuntimeOverlayObjectSuffix}");
        return overlayTransform != null ? overlayTransform.GetComponent<SpriteRenderer>() : null;
    }

    private SpriteRenderer CreateRuntimeOverlayRenderer(SpriteRenderer sourceRenderer)
    {
        if (sourceRenderer == null)
            return null;

        GameObject overlayObject = new($"{sourceRenderer.name}{RuntimeOverlayObjectSuffix}");
        overlayObject.hideFlags = HideFlags.DontSave;
        overlayObject.transform.SetParent(sourceRenderer.transform, false);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;

        SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;
        overlayRenderer.sharedMaterial = _unitVisualMaterial;
        return overlayRenderer;
    }

    private bool IsRuntimeOverlayRenderer(SpriteRenderer sourceRenderer, SpriteRenderer overlayRenderer)
    {
        if (sourceRenderer == null || overlayRenderer == null)
            return false;

        return overlayRenderer.transform.parent == sourceRenderer.transform &&
               overlayRenderer.name == $"{sourceRenderer.name}{RuntimeOverlayObjectSuffix}";
    }

    private bool CanRenderOverlay()
    {
        if (_rendererBindings.Count > 0)
        {
            _hasLoggedMissingOverlayWarning = false;
            return true;
        }

        if (!_hasLoggedMissingOverlayWarning)
        {
            Debug.LogWarning(
                $"[UnitVisualMaterialController] '{name}' has no valid source/overlay SpriteRenderer pair assigned. " +
                "Visual feedback disabled and source renderers will remain untouched.",
                this);
            _hasLoggedMissingOverlayWarning = true;
        }

        return false;
    }

    private void EnsureOverlayMaterial()
    {
        if (_unitVisualMaterial == null)
        {
            if (!_hasLoggedMissingMaterialWarning)
            {
                Debug.LogWarning($"[UnitVisualMaterialController] '{name}' has no unit visual material assigned. Visual feedback disabled.", this);
                _hasLoggedMissingMaterialWarning = true;
            }

            return;
        }

        _hasLoggedMissingMaterialWarning = false;

        for (int i = 0; i < _rendererBindings.Count; i++)
        {
            SpriteRenderer overlayRenderer = _rendererBindings[i].OverlayRenderer;
            if (overlayRenderer != null && overlayRenderer.sharedMaterial != _unitVisualMaterial)
                overlayRenderer.sharedMaterial = _unitVisualMaterial;
        }
    }

    private void SyncOverlayRenderers()
    {
        for (int i = 0; i < _rendererBindings.Count; i++)
            SyncOverlayRenderer(_rendererBindings[i].SourceRenderer, _rendererBindings[i].OverlayRenderer);
    }

    private void SyncOverlayRenderer(SpriteRenderer sourceRenderer, SpriteRenderer overlayRenderer)
    {
        if (sourceRenderer == null || overlayRenderer == null)
            return;

        overlayRenderer.sprite = sourceRenderer.sprite;
        overlayRenderer.flipX = sourceRenderer.flipX;
        overlayRenderer.flipY = sourceRenderer.flipY;
        overlayRenderer.drawMode = sourceRenderer.drawMode;
        overlayRenderer.size = sourceRenderer.size;
        overlayRenderer.tileMode = sourceRenderer.tileMode;
        overlayRenderer.adaptiveModeThreshold = sourceRenderer.adaptiveModeThreshold;
        overlayRenderer.maskInteraction = sourceRenderer.maskInteraction;
        overlayRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
        overlayRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = sourceRenderer.sortingOrder + _overlaySortingOrderOffset;
    }

    private void DisableOverlay(bool clearPropertyBlock = false)
    {
        if (_rendererBindings.Count == 0)
            return;

        for (int i = 0; i < _rendererBindings.Count; i++)
        {
            SpriteRenderer overlayRenderer = _rendererBindings[i].OverlayRenderer;
            if (overlayRenderer == null)
                continue;

            if (clearPropertyBlock)
            {
                EnsurePropertyBlock();
                overlayRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.Clear();
                overlayRenderer.SetPropertyBlock(_propertyBlock);
            }

            overlayRenderer.enabled = false;
        }

        LogDebug($"[UnitVisualMaterialController] '{name}' overlay disabled.");
    }

    private void LateUpdate()
    {
        if (!CanRenderOverlay())
            return;

        SyncOverlayRenderers();
    }

    private void DestroyRuntimeOverlayRenderers()
    {
        for (int i = 0; i < _rendererBindings.Count; i++)
        {
            RendererBinding binding = _rendererBindings[i];
            if (!binding.RuntimeOverlay || binding.OverlayRenderer == null)
                continue;

            GameObject overlayObject = binding.OverlayRenderer.gameObject;
            if (Application.isPlaying)
                Destroy(overlayObject);
            else
                DestroyImmediate(overlayObject);
        }

        _rendererBindings.Clear();
    }

    private string FormatRendererBindingsForDebug()
    {
        if (_rendererBindings.Count == 0)
            return "none";

        List<string> entries = new(_rendererBindings.Count);
        for (int i = 0; i < _rendererBindings.Count; i++)
        {
            RendererBinding binding = _rendererBindings[i];
            string sourceName = binding.SourceRenderer != null ? binding.SourceRenderer.name : "NULL";
            string overlayName = binding.OverlayRenderer != null ? binding.OverlayRenderer.name : "NULL";
            entries.Add($"{sourceName}->{overlayName}");
        }

        return string.Join(", ", entries);
    }

    private void EnsureDefaultBindings(bool forceReset = false)
    {
        if (!forceReset && _stateBindings != null && _stateBindings.Length > 0 && HasBinding(UnitVisualMaterialState.Normal))
            return;

        _stateBindings = new[]
        {
            CreateBinding(UnitVisualMaterialState.Normal, Color.clear, 0f),
            CreateBinding(UnitVisualMaterialState.Buff, new Color(0.35f, 0.85f, 1f, 0.32f), 1f),
            CreateBinding(UnitVisualMaterialState.HealOverTime, new Color(0.35f, 1f, 0.55f, 0.32f), 1f),
            CreateBinding(UnitVisualMaterialState.Debuff, new Color(0.95f, 0.45f, 0.78f, 0.34f), 1f),
            CreateBinding(UnitVisualMaterialState.DamageOverTime, new Color(1f, 0.38f, 0.38f, 0.36f), 1f),
            CreateBinding(UnitVisualMaterialState.DamageOverTimePermanent, new Color(0.62f, 0.2f, 0.9f, 0.4f), 1f),
            CreateBinding(UnitVisualMaterialState.Stun, new Color(1f, 0.9f, 0.25f, 0.4f), 1f),
            CreateBinding(UnitVisualMaterialState.Dead, new Color(0.32f, 0.32f, 0.36f, 0.6f), 1f),
            CreateBinding(UnitVisualMaterialState.Recruitable, new Color(0.38f, 1f, 0.76f, 0.48f), 1f),
            CreateBinding(UnitVisualMaterialState.SoulAbsorbed, new Color(0.24f, 0.24f, 0.26f, 0.72f), 1f)
        };
    }

    private bool HasBinding(UnitVisualMaterialState state)
    {
        if (_stateBindings == null)
            return false;

        for (int i = 0; i < _stateBindings.Length; i++)
        {
            if (_stateBindings[i].State == state)
                return true;
        }

        return false;
    }

    private static VisualStateBinding CreateBinding(UnitVisualMaterialState state, Color overlayColor, float overlayAmount)
    {
        return new VisualStateBinding(state, overlayColor, overlayAmount);
    }

    private void EnsureRuntimeState()
    {
        if (_flashColorPropertyId == 0)
            _flashColorPropertyId = Shader.PropertyToID(_flashColorPropertyName);

        if (_flashAmountPropertyId == 0)
            _flashAmountPropertyId = Shader.PropertyToID(_flashAmountPropertyName);

        if (_overlayColorPropertyId == 0)
            _overlayColorPropertyId = Shader.PropertyToID(_overlayColorPropertyName);

        if (_overlayAmountPropertyId == 0)
            _overlayAmountPropertyId = Shader.PropertyToID(_overlayAmountPropertyName);

        EnsurePropertyBlock();
    }

    private void EnsurePropertyBlock()
    {
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();
    }
}

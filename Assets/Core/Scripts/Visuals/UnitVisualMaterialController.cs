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
        _flashColorPropertyId = Shader.PropertyToID(_flashColorPropertyName);
        _flashAmountPropertyId = Shader.PropertyToID(_flashAmountPropertyName);
        _overlayColorPropertyId = Shader.PropertyToID(_overlayColorPropertyName);
        _overlayAmountPropertyId = Shader.PropertyToID(_overlayAmountPropertyName);
        _propertyBlock = new MaterialPropertyBlock();

        ResolveRendererReferences();
        EnsureOverlayMaterial();
        ApplyVisualState();
    }

    private void OnEnable()
    {
        EnsureDefaultBindings();
        ResolveRendererReferences();
        EnsureOverlayMaterial();
        ApplyVisualState();
    }

    private void OnDisable()
    {
        _blinkOverrideActive = false;
        DisableOverlay();
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
        ResolveRendererReferences();
        if (!CanRenderOverlay())
        {
            DisableOverlay();
            return;
        }
        SyncOverlayRenderer();

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

        _overlayRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.Clear();
        _propertyBlock.SetColor(_overlayColorPropertyId, binding.OverlayColor);
        _propertyBlock.SetFloat(_overlayAmountPropertyId, binding.OverlayAmount);
        _propertyBlock.SetColor(_flashColorPropertyId, _blinkColor);
        _propertyBlock.SetFloat(_flashAmountPropertyId, _blinkOverrideActive ? _blinkAmount : 0f);
        _overlayRenderer.SetPropertyBlock(_propertyBlock);
        _overlayRenderer.enabled = true;

        LogDebug($"[UnitVisualMaterialController] '{name}' applied state={_baseState}, blink={_blinkOverrideActive}, overlay=on.");
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

        if (_debugLogs && !_hasLoggedResolvedRenderers)
        {
            _hasLoggedResolvedRenderers = true;
            string sourceName = _sourceRenderer != null ? _sourceRenderer.name : "NULL";
            string overlayName = _overlayRenderer != null ? _overlayRenderer.name : "NULL";
            Debug.Log($"[UnitVisualMaterialController] '{name}' sourceRenderer={sourceName}, overlayRenderer={overlayName}.", this);
        }
    }

    private void AutoAssignSafeRendererReferences()
    {
        _sourceRenderer ??= GetComponent<SpriteRenderer>();
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

    private bool CanRenderOverlay()
    {
        if (_sourceRenderer == null)
        {
            if (!_hasLoggedMissingOverlayWarning)
            {
                Debug.LogWarning($"[UnitVisualMaterialController] '{name}' has no source SpriteRenderer assigned. Visual feedback disabled.", this);
                _hasLoggedMissingOverlayWarning = true;
            }

            return false;
        }

        if (_overlayRenderer == null)
        {
            if (!_hasLoggedMissingOverlayWarning)
            {
                Debug.LogWarning($"[UnitVisualMaterialController] '{name}' has no overlay SpriteRenderer assigned. Visual feedback disabled and source renderer will remain untouched.", this);
                _hasLoggedMissingOverlayWarning = true;
            }

            return false;
        }

        if (ReferenceEquals(_overlayRenderer, _sourceRenderer))
        {
            if (!_hasLoggedMissingOverlayWarning)
            {
                Debug.LogWarning($"[UnitVisualMaterialController] '{name}' overlay renderer matches source renderer. Visual feedback disabled to keep source renderer untouched.", this);
                _hasLoggedMissingOverlayWarning = true;
            }

            return false;
        }

        _hasLoggedMissingOverlayWarning = false;
        return true;
    }

    private void EnsureOverlayMaterial()
    {
        if (_overlayRenderer == null)
            return;

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

        if (_overlayRenderer.sharedMaterial != _unitVisualMaterial)
            _overlayRenderer.sharedMaterial = _unitVisualMaterial;
    }

    private void SyncOverlayRenderer()
    {
        if (_sourceRenderer == null || _overlayRenderer == null)
            return;

        _overlayRenderer.sprite = _sourceRenderer.sprite;
        _overlayRenderer.flipX = _sourceRenderer.flipX;
        _overlayRenderer.flipY = _sourceRenderer.flipY;
        _overlayRenderer.drawMode = _sourceRenderer.drawMode;
        _overlayRenderer.size = _sourceRenderer.size;
        _overlayRenderer.tileMode = _sourceRenderer.tileMode;
        _overlayRenderer.adaptiveModeThreshold = _sourceRenderer.adaptiveModeThreshold;
        _overlayRenderer.maskInteraction = _sourceRenderer.maskInteraction;
        _overlayRenderer.spriteSortPoint = _sourceRenderer.spriteSortPoint;
        _overlayRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
        _overlayRenderer.sortingOrder = _sourceRenderer.sortingOrder + _overlaySortingOrderOffset;
    }

    private void DisableOverlay(bool clearPropertyBlock = false)
    {
        if (_overlayRenderer == null)
            return;

        if (clearPropertyBlock)
        {
            _overlayRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
            _overlayRenderer.SetPropertyBlock(_propertyBlock);
        }

        _overlayRenderer.enabled = false;
        LogDebug($"[UnitVisualMaterialController] '{name}' overlay disabled.");
    }

    private void LateUpdate()
    {
        if (!CanRenderOverlay())
            return;

        SyncOverlayRenderer();
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
}

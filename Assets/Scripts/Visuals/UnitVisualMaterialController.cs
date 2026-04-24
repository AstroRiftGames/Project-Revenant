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

    private SpriteRenderer[] _spriteRenderers;
    private MaterialPropertyBlock _propertyBlock;
    private int _flashColorPropertyId;
    private int _flashAmountPropertyId;
    private int _overlayColorPropertyId;
    private int _overlayAmountPropertyId;
    private UnitVisualMaterialState _baseState;
    private bool _blinkOverrideActive;

    private void Reset()
    {
        EnsureDefaultBindings(forceReset: true);
    }

    private void Awake()
    {
        EnsureDefaultBindings();
        _flashColorPropertyId = Shader.PropertyToID(_flashColorPropertyName);
        _flashAmountPropertyId = Shader.PropertyToID(_flashAmountPropertyName);
        _overlayColorPropertyId = Shader.PropertyToID(_overlayColorPropertyName);
        _overlayAmountPropertyId = Shader.PropertyToID(_overlayAmountPropertyName);
        _propertyBlock = new MaterialPropertyBlock();

        RefreshRendererCache();
        EnsureVisualMaterial();
        ApplyVisualState();
    }

    private void OnEnable()
    {
        EnsureDefaultBindings();
        RefreshRendererCache();
        EnsureVisualMaterial();
        ApplyVisualState();
    }

    private void OnDisable()
    {
        _blinkOverrideActive = false;
        ApplyVisualState();
    }

    private void OnValidate()
    {
        EnsureDefaultBindings();
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
        RefreshRendererCache();
        EnsureVisualMaterial();
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (_spriteRenderers == null)
            return;

        VisualStateBinding binding = ResolveBinding(_baseState);

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_overlayColorPropertyId, binding.OverlayColor);
            _propertyBlock.SetFloat(_overlayAmountPropertyId, binding.OverlayAmount);
            _propertyBlock.SetColor(_flashColorPropertyId, _blinkColor);
            _propertyBlock.SetFloat(_flashAmountPropertyId, _blinkOverrideActive ? _blinkAmount : 0f);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        LogDebug($"[UnitVisualMaterialController] '{name}' applied state={_baseState}, blink={_blinkOverrideActive}.");
    }

    private void RefreshRendererCache()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
    }

    private void EnsureVisualMaterial()
    {
        if (_unitVisualMaterial == null || _spriteRenderers == null)
        {
            if (_unitVisualMaterial == null)
                Debug.LogWarning($"[UnitVisualMaterialController] '{name}' has no unit visual material assigned.", this);

            return;
        }

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null || spriteRenderer.sharedMaterial == _unitVisualMaterial)
                continue;

            spriteRenderer.sharedMaterial = _unitVisualMaterial;
        }
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

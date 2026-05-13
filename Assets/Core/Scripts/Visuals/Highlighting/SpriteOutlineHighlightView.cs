using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteOutlineHighlightView : MonoBehaviour
{
    private static readonly int HighlightEnabledId = Shader.PropertyToID("_HighlightEnabled");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OverlayScaleId = Shader.PropertyToID("_OverlayScale");
    private static readonly int UvCenterId = Shader.PropertyToID("_UvCenter");
    private static readonly int UvRectId = Shader.PropertyToID("_UvRect");
    private const string OverlayObjectSuffix = "_HighlightOverlay";

    private sealed class OverlayEntry
    {
        public SpriteRenderer SourceRenderer;
        public SpriteRenderer OverlayRenderer;
        public Transform OverlayTransform;
        public Material InstanceMaterial;
    }

    [Header("Targets")]
    [SerializeField] private SpriteRenderer[] _targetRenderers;
    [SerializeField] private bool _autoCollectChildRenderers = true;
    [SerializeField] private bool _includeInactiveChildren = true;

    [Header("Material")]
    [SerializeField] private Material _outlineMaterial;

    [Header("Outline")]
    [SerializeField] private Color _outlineColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] [Min(0f)] private float _outlineThickness = 1f;
    [SerializeField] private bool _highlightedOnEnable;
    [SerializeField] [Min(1)] private int _sortingOrderOffset = 1;

    private MaterialPropertyBlock _propertyBlock;
    private readonly List<OverlayEntry> _overlayEntries = new();
    private bool _isHighlighted;

    public bool IsHighlighted => _isHighlighted;

    private void Reset()
    {
        CollectRenderers();
    }

    private void Awake()
    {
        EnsurePropertyBlock();
        EnsureRenderers();
        EnsureOverlayRenderers();
        _isHighlighted = _highlightedOnEnable;
        SyncOverlayRenderers();
        ApplyProperties(forceHighlighted: _isHighlighted);
    }

    private void OnEnable()
    {
        EnsureOverlayRenderers();
        SyncOverlayRenderers();
        ApplyProperties();
    }

    private void OnDisable()
    {
        ApplyProperties(forceHighlighted: false);
    }

    private void OnValidate()
    {
        _outlineThickness = Mathf.Max(0f, _outlineThickness);

        if (!Application.isPlaying)
        {
            EnsureRenderers();
        }

        if (Application.isPlaying)
            SyncOverlayRenderers();

        ApplyProperties(forceHighlighted: Application.isPlaying ? _isHighlighted : _highlightedOnEnable);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (_isHighlighted == isHighlighted)
            return;

        _isHighlighted = isHighlighted;
        ApplyProperties();
    }

    public void SetOutlineColor(Color outlineColor)
    {
        _outlineColor = outlineColor;
        ApplyProperties();
    }

    public void SetOutlineThickness(float outlineThickness)
    {
        _outlineThickness = Mathf.Max(0f, outlineThickness);
        ApplyProperties();
    }

    public void RefreshVisualState()
    {
        SyncOverlayRenderers();
        ApplyProperties();
    }

    private void LateUpdate()
    {
        SyncOverlayRenderers();
    }

    private void OnDestroy()
    {
        DestroyOverlayRenderers();
    }

    private void EnsureRenderers()
    {
        if (_targetRenderers == null || _targetRenderers.Length == 0 || _autoCollectChildRenderers)
            CollectRenderers();
    }

    private void CollectRenderers()
    {
        _targetRenderers = GetComponentsInChildren<SpriteRenderer>(_includeInactiveChildren);
    }

    private void EnsureOverlayRenderers()
    {
        if (_targetRenderers == null)
            return;

        for (int i = 0; i < _targetRenderers.Length; i++)
        {
            SpriteRenderer sourceRenderer = _targetRenderers[i];
            if (sourceRenderer == null || HasOverlayEntry(sourceRenderer))
                continue;

            GameObject overlayObject = new($"{sourceRenderer.name}{OverlayObjectSuffix}");
            overlayObject.hideFlags = HideFlags.DontSave;
            overlayObject.transform.SetParent(sourceRenderer.transform, false);
            overlayObject.transform.localPosition = Vector3.zero;
            overlayObject.transform.localRotation = Quaternion.identity;
            overlayObject.transform.localScale = Vector3.one;

            SpriteRenderer overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            overlayRenderer.enabled = false;

            Material instanceMaterial = _outlineMaterial != null ? new Material(_outlineMaterial) : null;
            overlayRenderer.sharedMaterial = instanceMaterial;

            _overlayEntries.Add(new OverlayEntry
            {
                SourceRenderer = sourceRenderer,
                OverlayRenderer = overlayRenderer,
                OverlayTransform = overlayObject.transform,
                InstanceMaterial = instanceMaterial
            });
        }
    }

    private void ApplyProperties(bool? forceHighlighted = null)
    {
        if (_overlayEntries.Count == 0)
            return;

        bool highlighted = forceHighlighted.HasValue ? forceHighlighted.Value : _isHighlighted;

        for (int i = _overlayEntries.Count - 1; i >= 0; i--)
        {
            OverlayEntry entry = _overlayEntries[i];
            if (entry.SourceRenderer == null || entry.OverlayRenderer == null)
                continue;

            bool shouldRender =
                highlighted &&
                entry.InstanceMaterial != null &&
                entry.SourceRenderer.enabled &&
                entry.SourceRenderer.sprite != null;

            entry.OverlayRenderer.enabled = shouldRender;
            if (!shouldRender)
                continue;

            // Write directly to the per-instance material — works with SRP Batcher.
            entry.InstanceMaterial.SetColor(OutlineColorId, _outlineColor);
            entry.InstanceMaterial.SetFloat(OutlineThicknessId, _outlineThickness);
        }
    }

    private void EnsurePropertyBlock()
    {
        _propertyBlock ??= new MaterialPropertyBlock();
    }

    private void SyncOverlayRenderers()
    {
        if (_overlayEntries.Count == 0)
            return;

        for (int i = _overlayEntries.Count - 1; i >= 0; i--)
        {
            OverlayEntry entry = _overlayEntries[i];
            if (entry.SourceRenderer == null)
            {
                DestroyOverlayRenderer(entry);
                _overlayEntries.RemoveAt(i);
                continue;
            }

            if (entry.OverlayRenderer == null || entry.OverlayTransform == null)
            {
                _overlayEntries.RemoveAt(i);
                continue;
            }

            SyncOverlayRenderer(entry);
        }
    }

    private void SyncOverlayRenderer(OverlayEntry entry)
    {
        SpriteRenderer sourceRenderer = entry.SourceRenderer;
        SpriteRenderer overlayRenderer = entry.OverlayRenderer;

        // Keep using the instance material — don't override back to shared.
        overlayRenderer.sprite = sourceRenderer.sprite;
        overlayRenderer.drawMode = sourceRenderer.drawMode;
        overlayRenderer.size = sourceRenderer.size;
        overlayRenderer.tileMode = sourceRenderer.tileMode;
        overlayRenderer.adaptiveModeThreshold = sourceRenderer.adaptiveModeThreshold;
        overlayRenderer.flipX = sourceRenderer.flipX;
        overlayRenderer.flipY = sourceRenderer.flipY;
        overlayRenderer.maskInteraction = sourceRenderer.maskInteraction;
        overlayRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
        overlayRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = sourceRenderer.sortingOrder + _sortingOrderOffset;
        overlayRenderer.color = new Color(1f, 1f, 1f, sourceRenderer.color.a);
        
        // Pass UV and scaling data to the material to generate synthetic padding without stretching the art
        if (entry.InstanceMaterial != null && sourceRenderer.sprite != null)
        {
            Vector4 uvRect = UnityEngine.Sprites.DataUtility.GetOuterUV(sourceRenderer.sprite);
            // uvRect is (xMin, yMin, xMax, yMax)
            Vector4 uvCenter = new Vector4((uvRect.x + uvRect.z) * 0.5f, (uvRect.y + uvRect.w) * 0.5f, 0, 0);
            
            Vector3 scale = ResolveOverlayScale(sourceRenderer);
            Vector4 shaderScale = new Vector4(scale.x, scale.y, 0, 0);

            entry.InstanceMaterial.SetVector(OverlayScaleId, shaderScale);
            entry.InstanceMaterial.SetVector(UvCenterId, uvCenter);
            entry.InstanceMaterial.SetVector(UvRectId, uvRect);
        }

        entry.OverlayTransform.localPosition = Vector3.zero;
        entry.OverlayTransform.localRotation = Quaternion.identity;
        entry.OverlayTransform.localScale = ResolveOverlayScale(sourceRenderer);
    }

    private Vector3 ResolveOverlayScale(SpriteRenderer sourceRenderer)
    {
        Vector2 sourceSize = ResolveSourceSize(sourceRenderer);
        if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            return Vector3.one;

        float pixelsPerUnit = sourceRenderer.sprite != null ? sourceRenderer.sprite.pixelsPerUnit : 100f;
        // Expand by double the outline thickness (padding on both sides)
        float expansionUnits = (_outlineThickness * 2f) / pixelsPerUnit;
        
        float scaleX = (sourceSize.x + expansionUnits) / sourceSize.x;
        float scaleY = (sourceSize.y + expansionUnits) / sourceSize.y;
        return new Vector3(scaleX, scaleY, 1f);
    }

    private static Vector2 ResolveSourceSize(SpriteRenderer sourceRenderer)
    {
        if (sourceRenderer == null)
            return Vector2.zero;

        if (sourceRenderer.drawMode == SpriteDrawMode.Simple)
            return sourceRenderer.sprite != null ? sourceRenderer.sprite.bounds.size : Vector2.zero;

        return sourceRenderer.size;
    }

    private bool HasOverlayEntry(SpriteRenderer sourceRenderer)
    {
        for (int i = 0; i < _overlayEntries.Count; i++)
        {
            if (_overlayEntries[i].SourceRenderer == sourceRenderer)
                return true;
        }

        return false;
    }

    private void DestroyOverlayRenderers()
    {
        for (int i = 0; i < _overlayEntries.Count; i++)
            DestroyOverlayRenderer(_overlayEntries[i]);

        _overlayEntries.Clear();
    }

    private static void DestroyOverlayRenderer(OverlayEntry entry)
    {
        if (entry?.OverlayTransform == null)
            return;

        if (entry.InstanceMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(entry.InstanceMaterial);
            else
                DestroyImmediate(entry.InstanceMaterial);
        }

        if (Application.isPlaying)
            Destroy(entry.OverlayTransform.gameObject);
        else
            DestroyImmediate(entry.OverlayTransform.gameObject);
    }
}

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteOutlineHighlightView : MonoBehaviour
{
    private static readonly int HighlightEnabledId = Shader.PropertyToID("_HighlightEnabled");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    private const string OverlayObjectSuffix = "_HighlightOverlay";

    private sealed class OverlayEntry
    {
        public SpriteRenderer SourceRenderer;
        public SpriteRenderer OverlayRenderer;
        public Transform OverlayTransform;
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
            overlayRenderer.sharedMaterial = _outlineMaterial;

            _overlayEntries.Add(new OverlayEntry
            {
                SourceRenderer = sourceRenderer,
                OverlayRenderer = overlayRenderer,
                OverlayTransform = overlayObject.transform
            });
        }
    }

    private void ApplyProperties(bool? forceHighlighted = null)
    {
        if (_overlayEntries.Count == 0)
            return;

        EnsurePropertyBlock();
        float highlightEnabled = forceHighlighted ?? _isHighlighted ? 1f : 0f;

        for (int i = _overlayEntries.Count - 1; i >= 0; i--)
        {
            OverlayEntry entry = _overlayEntries[i];
            if (entry.SourceRenderer == null || entry.OverlayRenderer == null)
                continue;

            SpriteRenderer overlayRenderer = entry.OverlayRenderer;
            bool shouldRender =
                highlightEnabled > 0.5f &&
                _outlineMaterial != null &&
                entry.SourceRenderer.enabled &&
                entry.SourceRenderer.sprite != null;

            overlayRenderer.enabled = shouldRender;
            if (!shouldRender)
                continue;

            overlayRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(HighlightEnabledId, highlightEnabled);
            _propertyBlock.SetColor(OutlineColorId, _outlineColor);
            _propertyBlock.SetFloat(OutlineThicknessId, _outlineThickness);
            overlayRenderer.SetPropertyBlock(_propertyBlock);
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

        overlayRenderer.sharedMaterial = _outlineMaterial;
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
        overlayRenderer.sortingOrder = sourceRenderer.sortingOrder - _sortingOrderOffset;
        overlayRenderer.color = new Color(1f, 1f, 1f, sourceRenderer.color.a);

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
        float expansionUnits = _outlineThickness / pixelsPerUnit;
        float scaleX = (sourceSize.x + (expansionUnits * 2f)) / sourceSize.x;
        float scaleY = (sourceSize.y + (expansionUnits * 2f)) / sourceSize.y;
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

        if (Application.isPlaying)
            Destroy(entry.OverlayTransform.gameObject);
        else
            DestroyImmediate(entry.OverlayTransform.gameObject);
    }
}

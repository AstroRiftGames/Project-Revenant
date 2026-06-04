using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillDebugVfxInstance : MonoBehaviour
{
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private bool _followTarget;
    [SerializeField] private Unit _target;
    [SerializeField] private Vector3 _worldPosition;
    [SerializeField] private Material _prototypeMaterial;
    [SerializeField] private string _sortingLayerName = "UI";
    [SerializeField] private int _sortingOrder = 5000;

    private static Sprite _whiteSprite;
    private static Material _fallbackMaterial;

    private readonly List<SpriteRenderer> _spriteRenderers = new();
    private readonly List<LineRenderer> _lineRenderers = new();

    private float _elapsed;
    private float _configuredDuration;

    private void Update()
    {
        if (_followTarget && _target != null)
            transform.position = _target.Position;
        else
            transform.position = _worldPosition;

        _elapsed += Mathf.Max(0f, Time.deltaTime);
        if (_configuredDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float normalizedLifetime = Mathf.Clamp01(_elapsed / _configuredDuration);
        ApplyAlpha(1f - normalizedLifetime);

        if (_elapsed >= _configuredDuration)
            Destroy(gameObject);
    }

    public void ConfigurePoint(Vector3 worldPosition, Unit target, bool followTarget, float size, Color color, float duration, float rotationZ = 0f)
    {
        Prepare(worldPosition, target, followTarget, duration);
        CreateSpritePrimitive("Point", size, size, rotationZ, color, Vector3.zero);
    }

    public void ConfigureDiamond(Vector3 worldPosition, Unit target, bool followTarget, float size, Color color, float duration)
    {
        ConfigurePoint(worldPosition, target, followTarget, size, color, duration, 45f);
    }

    public void ConfigureCross(Vector3 worldPosition, Unit target, bool followTarget, float size, float thickness, Color color, float duration)
    {
        Prepare(worldPosition, target, followTarget, duration);
        CreateSpritePrimitive("Cross_H", size, thickness, 0f, color, Vector3.zero);
        CreateSpritePrimitive("Cross_V", thickness, size, 0f, color, Vector3.zero);
    }

    public void ConfigureRing(Vector3 worldPosition, Unit target, bool followTarget, float radius, float width, Color color, float duration)
    {
        Prepare(worldPosition, target, followTarget, duration);

        LineRenderer lineRenderer = CreateLineRenderer("Ring", color, width, true);
        const int segmentCount = 28;
        lineRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = ((float)i / segmentCount) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    public void ConfigureLine(Vector3 startWorldPosition, Vector3 endWorldPosition, float width, Color color, float duration)
    {
        Prepare(startWorldPosition, null, false, duration);

        LineRenderer lineRenderer = CreateLineRenderer("Line", color, width, false);
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, endWorldPosition - startWorldPosition);
    }

    public void ConfigureArrow(Vector3 startWorldPosition, Vector3 endWorldPosition, float width, Color color, float duration)
    {
        Prepare(startWorldPosition, null, false, duration);

        Vector3 localEnd = endWorldPosition - startWorldPosition;
        Vector3 direction = localEnd.normalized;
        if (direction.sqrMagnitude < Mathf.Epsilon)
            direction = Vector3.right;

        Vector3 arrowBase = localEnd - direction * Mathf.Max(width * 6f, 0.15f);
        Vector3 arrowLeft = arrowBase + Quaternion.Euler(0f, 0f, 150f) * direction * Mathf.Max(width * 4f, 0.12f);
        Vector3 arrowRight = arrowBase + Quaternion.Euler(0f, 0f, -150f) * direction * Mathf.Max(width * 4f, 0.12f);

        LineRenderer lineRenderer = CreateLineRenderer("Arrow", color, width, false);
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, localEnd);
        lineRenderer.SetPosition(2, arrowLeft);
        lineRenderer.SetPosition(3, localEnd);
        lineRenderer.SetPosition(4, arrowRight);
    }

    private void Prepare(Vector3 worldPosition, Unit target, bool followTarget, float duration)
    {
        ClearVisuals();

        _elapsed = 0f;
        _configuredDuration = Mathf.Max(0.05f, duration > 0f ? duration : _duration);
        _target = target;
        _followTarget = followTarget && target != null;
        _worldPosition = _followTarget && target != null ? target.Position : worldPosition;
        transform.position = _worldPosition;
    }

    private void ClearVisuals()
    {
        _spriteRenderers.Clear();
        _lineRenderers.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private SpriteRenderer CreateSpritePrimitive(
        string childName,
        float width,
        float height,
        float rotationZ,
        Color color,
        Vector3 localPosition)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        child.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = ResolveWhiteSprite();
        spriteRenderer.color = color;
        spriteRenderer.sortingLayerName = ResolveSortingLayerName();
        spriteRenderer.sortingOrder = _sortingOrder;

        Material material = ResolveMaterial();
        if (material != null)
            spriteRenderer.sharedMaterial = material;

        _spriteRenderers.Add(spriteRenderer);
        return spriteRenderer;
    }

    private LineRenderer CreateLineRenderer(string childName, Color color, float width, bool loop)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.loop = loop;
        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.widthMultiplier = 1f;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.sortingLayerName = ResolveSortingLayerName();
        lineRenderer.sortingOrder = _sortingOrder;

        Material material = ResolveMaterial();
        if (material != null)
            lineRenderer.sharedMaterial = material;

        _lineRenderers.Add(lineRenderer);
        return lineRenderer;
    }

    private void ApplyAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        for (int i = 0; i < _lineRenderers.Count; i++)
        {
            LineRenderer lineRenderer = _lineRenderers[i];
            if (lineRenderer == null)
                continue;

            Color startColor = lineRenderer.startColor;
            Color endColor = lineRenderer.endColor;
            startColor.a = alpha;
            endColor.a = alpha;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
        }
    }

    private Material ResolveMaterial()
    {
        if (_prototypeMaterial != null)
            return _prototypeMaterial;

        if (_fallbackMaterial != null)
            return _fallbackMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return null;

        _fallbackMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return _fallbackMaterial;
    }

    private string ResolveSortingLayerName()
    {
        return string.IsNullOrWhiteSpace(_sortingLayerName)
            ? "UI"
            : _sortingLayerName;
    }

    private static Sprite ResolveWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "SkillDebugVfx_WhitePixel_Runtime";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _whiteSprite.name = "SkillDebugVfx_WhitePixel_Runtime";
        return _whiteSprite;
    }
}

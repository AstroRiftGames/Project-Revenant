using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TestVfxPlaceholder : MonoBehaviour
{
    private enum VfxType
    {
        Recruitment,
        EssenceConsume
    }

    [SerializeField] private VfxType _vfxType = VfxType.Recruitment;
    [SerializeField] private Color _color = Color.white;
    [SerializeField] private string _sortingLayerName = "UI";
    [SerializeField] private int _sortingOrder = 5000;

    private static Sprite _whiteSprite;
    private static Material _fallbackMaterial;

    private float _elapsed;
    private float _duration;

    private CorpseInteractionVfxContext _vfxContext;
    private bool _contextConfigured;

    // Visual elements
    private Transform _runeTransform;
    private SpriteRenderer _runeRenderer;

    private Transform _silhouetteTransform;
    private SpriteRenderer _silhouetteRenderer;

    private Transform _ghostTransform;
    private SpriteRenderer _ghostRenderer;

    private LineRenderer _drainingLine;

    private readonly List<Transform> _tendrils = new List<Transform>();
    private readonly List<SpriteRenderer> _tendrilRenderers = new List<SpriteRenderer>();
    private readonly List<Vector3> _tendrilStartOffsets = new List<Vector3>();

    private readonly List<Transform> _particles = new List<Transform>();
    private readonly List<SpriteRenderer> _particleRenderers = new List<SpriteRenderer>();
    private readonly List<Vector3> _particleLocalTargets = new List<Vector3>();
    private readonly List<Vector3> _particleStartLocalPositions = new List<Vector3>();

    public void ConfigureContext(CorpseInteractionVfxContext context)
    {
        _vfxContext = context;
        _contextConfigured = true;

        if (_vfxType == VfxType.Recruitment)
        {
            SetupRecruitmentVisuals();
        }
        else
        {
            SetupEssenceConsumeVisuals();
        }
    }

    private void Start()
    {
        _duration = (_vfxType == VfxType.Recruitment) ? 1.1f : 0.7f;
        gameObject.name = (_vfxType == VfxType.Recruitment) ? "TEST_VFX_Recruitment_Instance" : "TEST_VFX_EssenceConsume_Instance";
    }

    private void SetupRecruitmentVisuals()
    {
        // 1. Copy of the corpse sprite as a dark possessed silhouette
        SetupCorpseSilhouette(gameObject, new Color(0.08f, 0.03f, 0.15f, 0.8f), out _silhouetteRenderer, out _silhouetteTransform);

        // 2. Secondary lower rune ring underneath the corpse
        GameObject ringObj = new GameObject("RitualRune");
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localScale = Vector3.zero;
        _runeTransform = ringObj.transform;

        _runeRenderer = ringObj.AddComponent<SpriteRenderer>();
        _runeRenderer.sprite = ResolveWhiteSprite();
        _runeRenderer.color = new Color(0.3f, 0.1f, 0.5f, 0.4f);
        if (_contextConfigured)
        {
            _runeRenderer.sortingLayerID = _vfxContext.SortingLayerId;
            _runeRenderer.sortingOrder = _vfxContext.SortingOrder - 1;
        }
        else
        {
            _runeRenderer.sortingLayerName = _sortingLayerName;
            _runeRenderer.sortingOrder = _sortingOrder - 1;
        }
        _runeRenderer.sharedMaterial = ResolveMaterial();

        // 3. Possession tendrils/shadows entering the body
        for (int i = 0; i < 4; i++)
        {
            GameObject tendril = new GameObject($"Tendril_{i}");
            tendril.transform.SetParent(transform, false);
            tendril.transform.localScale = new Vector3(0.05f, 0.3f, 1f);

            SpriteRenderer sr = tendril.AddComponent<SpriteRenderer>();
            sr.sprite = ResolveWhiteSprite();
            sr.color = new Color(0.12f, 0.05f, 0.25f, 0.75f);
            if (_contextConfigured)
            {
                sr.sortingLayerID = _vfxContext.SortingLayerId;
                sr.sortingOrder = _vfxContext.SortingOrder + 2;
            }
            else
            {
                sr.sortingLayerName = _sortingLayerName;
                sr.sortingOrder = _sortingOrder + 2;
            }
            sr.sharedMaterial = ResolveMaterial();

            float angle = (i * Mathf.PI * 2f) / 4f;
            Vector3 startOffset = new Vector3(Mathf.Cos(angle) * 0.7f, Mathf.Sin(angle) * 0.7f + 0.3f, 0f);
            tendril.transform.localPosition = startOffset;
            tendril.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f);

            _tendrils.Add(tendril.transform);
            _tendrilRenderers.Add(sr);
            _tendrilStartOffsets.Add(startOffset);
        }
    }

    private void SetupEssenceConsumeVisuals()
    {
        // 1. Base silhouette representing the collapsing corpse physical shell
        SetupCorpseSilhouette(gameObject, new Color(0.15f, 0.08f, 0.2f, 0.85f), out _silhouetteRenderer, out _silhouetteTransform);

        // 2. Ghost/Soul silhouette that will be violently pulled out
        SetupCorpseSilhouette(gameObject, new Color(0.4f, 0.7f, 1f, 0.55f), out _ghostRenderer, out _ghostTransform);
        _ghostTransform.gameObject.name = "GhostSoul";

        // 3. Draining line connecting corpse to Necromancer
        GameObject lineObj = new GameObject("DrainingTendril");
        lineObj.transform.SetParent(transform, false);
        _drainingLine = lineObj.AddComponent<LineRenderer>();
        _drainingLine.useWorldSpace = false;
        _drainingLine.widthMultiplier = 1f;
        _drainingLine.startWidth = 0.06f;
        _drainingLine.endWidth = 0.02f;
        _drainingLine.startColor = new Color(0.3f, 0.6f, 0.9f, 0.7f);
        _drainingLine.endColor = new Color(0.5f, 0.2f, 0.8f, 0.4f);
        _drainingLine.sharedMaterial = ResolveMaterial();
        _drainingLine.positionCount = 2;
        _drainingLine.SetPosition(0, Vector3.zero);
        _drainingLine.SetPosition(1, _contextConfigured ? transform.InverseTransformPoint(_vfxContext.NecromancerPosition) : Vector3.up * 1.2f);

        // 4. Inward collapsing/flying essence particles
        Vector3 localEnd = _contextConfigured ? transform.InverseTransformPoint(_vfxContext.NecromancerPosition) : Vector3.up * 1.2f;
        for (int i = 0; i < 10; i++)
        {
            GameObject part = new GameObject($"EssencePart_{i}");
            part.transform.SetParent(transform, false);
            part.transform.localPosition = Vector3.zero;
            part.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);

            SpriteRenderer sr = part.AddComponent<SpriteRenderer>();
            sr.sprite = ResolveWhiteSprite();
            sr.color = new Color(_color.r, _color.g, _color.b, 0.8f);
            if (_contextConfigured)
            {
                sr.sortingLayerID = _vfxContext.SortingLayerId;
                sr.sortingOrder = _vfxContext.SortingOrder + 1;
            }
            else
            {
                sr.sortingLayerName = _sortingLayerName;
                sr.sortingOrder = _sortingOrder + 1;
            }
            sr.sharedMaterial = ResolveMaterial();

            _particles.Add(part.transform);
            _particleRenderers.Add(sr);
            _particleStartLocalPositions.Add(Vector3.zero);
            _particleLocalTargets.Add(localEnd);
        }
    }

    private void SetupCorpseSilhouette(GameObject parent, Color col, out SpriteRenderer sr, out Transform tr)
    {
        GameObject obj = new GameObject("CorpseSilhouette");
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = Vector3.zero;

        sr = obj.AddComponent<SpriteRenderer>();
        tr = obj.transform;

        if (_contextConfigured && _vfxContext.CorpseSprite != null)
        {
            sr.sprite = _vfxContext.CorpseSprite;
            tr.localScale = _vfxContext.CorpseScale;
            sr.sortingLayerID = _vfxContext.SortingLayerId;
            sr.sortingOrder = _vfxContext.SortingOrder + 1;
        }
        else
        {
            sr.sprite = ResolveWhiteSprite();
            tr.localScale = new Vector3(0.5f, 0.8f, 1f);
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder = _sortingOrder + 1;
        }

        sr.color = col;
        sr.sharedMaterial = ResolveMaterial();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _duration);

        if (_vfxType == VfxType.Recruitment)
        {
            AnimateRecruitment(progress);
        }
        else
        {
            AnimateEssenceConsume(progress);
        }
    }

    private void AnimateRecruitment(float progress)
    {
        if (_silhouetteTransform != null)
        {
            float shakeProgress = Mathf.Clamp01(progress / 0.7f);
            float shakeAmp = (1f - shakeProgress) * 0.05f + (progress < 0.7f ? 0.03f : 0f);
            _silhouetteTransform.localPosition = new Vector3(Random.Range(-shakeAmp, shakeAmp), Random.Range(-shakeAmp, shakeAmp), 0f);

            Color possessedColor = new Color(0.1f, 0.4f, 0.15f, 0.85f);
            if (progress >= 0.65f && progress <= 0.8f)
            {
                _silhouetteRenderer.color = Color.white;
            }
            else
            {
                _silhouetteRenderer.color = Color.Lerp(new Color(0.08f, 0.03f, 0.15f, 0.8f), possessedColor, progress);
            }
        }

        if (_runeTransform != null)
        {
            float scale = Mathf.Clamp01(progress / 0.3f) * 1.0f;
            _runeTransform.localScale = new Vector3(scale, scale * 0.35f, 1f);
            _runeTransform.Rotate(Vector3.forward, 45f * Time.deltaTime);

            Color c = _runeRenderer.color;
            c.a = (1f - progress) * 0.4f;
            _runeRenderer.color = c;
        }

        for (int i = 0; i < _tendrils.Count; i++)
        {
            if (_tendrils[i] == null) continue;
            float t = Mathf.Clamp01(progress / 0.7f);
            _tendrils[i].localPosition = Vector3.Lerp(_tendrilStartOffsets[i], Vector3.zero, t);
            
            if (i < _tendrilRenderers.Count && _tendrilRenderers[i] != null)
            {
                Color c = _tendrilRenderers[i].color;
                c.a = (1f - t) * 0.8f;
                _tendrilRenderers[i].color = c;
            }
        }
    }

    private void AnimateEssenceConsume(float progress)
    {
        Vector3 localEnd = _contextConfigured ? transform.InverseTransformPoint(_vfxContext.NecromancerPosition) : Vector3.up * 1.2f;

        if (_silhouetteTransform != null)
        {
            float tCollapse = Mathf.Clamp01(progress / 0.5f);
            Vector3 originalScale = _contextConfigured ? _vfxContext.CorpseScale : new Vector3(0.5f, 0.8f, 1f);
            _silhouetteTransform.localScale = new Vector3(originalScale.x, originalScale.y * (1f - tCollapse * 0.8f), originalScale.z);
            _silhouetteRenderer.color = Color.Lerp(new Color(0.15f, 0.08f, 0.2f, 0.85f), new Color(0.02f, 0.01f, 0.04f, 0f), tCollapse);
        }

        if (_ghostTransform != null)
        {
            Vector3 midPoint = (Vector3.zero + localEnd) * 0.5f + Vector3.up * 0.7f;
            Vector3 m1 = Vector3.Lerp(Vector3.zero, midPoint, progress);
            Vector3 m2 = Vector3.Lerp(midPoint, localEnd, progress);
            Vector3 currentPos = Vector3.Lerp(m1, m2, progress);
            _ghostTransform.localPosition = currentPos;

            Vector3 moveDirection = (localEnd - currentPos).normalized;
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            _ghostTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            float stretchFactor = 1.0f + Mathf.Sin(progress * Mathf.PI) * 0.8f;
            Vector3 baseScale = _contextConfigured ? _vfxContext.CorpseScale : new Vector3(0.5f, 0.8f, 1f);
            _ghostTransform.localScale = new Vector3(baseScale.x * (1f - progress * 0.5f), baseScale.y * stretchFactor * (1f - progress), baseScale.z);

            Color gc = _ghostRenderer.color;
            gc.a = (1f - progress) * 0.6f;
            _ghostRenderer.color = gc;
        }

        if (_drainingLine != null)
        {
            _drainingLine.startColor = new Color(0.2f, 0.5f, 0.9f, (1f - progress) * 0.7f);
            _drainingLine.endColor = new Color(0.4f, 0.1f, 0.7f, (1f - progress) * 0.4f);
        }

        for (int i = 0; i < _particles.Count; i++)
        {
            if (_particles[i] == null) continue;
            float t = Mathf.Clamp01((progress - (i * 0.03f)) / (1f - (i * 0.03f)));
            Vector3 basePos = Vector3.Lerp(Vector3.zero, localEnd, t);
            Vector3 arcOffset = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.5f) + new Vector3(Mathf.Sin(i * 1.5f), Mathf.Cos(i * 1.5f), 0f) * (Mathf.Sin(t * Mathf.PI) * 0.25f);
            
            _particles[i].localPosition = basePos + arcOffset;

            if (i < _particleRenderers.Count && _particleRenderers[i] != null)
            {
                Color c = _particleRenderers[i].color;
                c.a = (1f - t) * 0.8f;
                _particleRenderers[i].color = c;
            }
            _particles[i].localScale = Vector3.one * (0.08f * (1f - t));
        }
    }

    private static Sprite ResolveWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "TestVfxPlaceholder_WhitePixel";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _whiteSprite.name = "TestVfxPlaceholder_WhitePixel";
        return _whiteSprite;
    }

    private static Material ResolveMaterial()
    {
        if (_fallbackMaterial != null)
            return _fallbackMaterial;

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        if (shader == null)
            return null;

        _fallbackMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return _fallbackMaterial;
    }
}

using UnityEngine;
using System.Collections.Generic;

public class SkillCastPlaceholderPresenter : MonoBehaviour
{
    private static Material _sharedMaterial;
    private static bool _isSubscribed;

    private struct ActiveCastVisuals
    {
        public GameObject CasterRing;
        public GameObject TelegraphRing;
    }

    private static readonly Dictionary<Unit, ActiveCastVisuals> _activeVisuals = new Dictionary<Unit, ActiveCastVisuals>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_isSubscribed) return;
        SkillCaster.AnySkillCastStarted += HandleSkillCastStarted;
        SkillCaster.AnySkillCastInterrupted += HandleSkillCastInterrupted;
        SkillCaster.AnySkillCastCompleted += HandleSkillCastCompleted;
        _isSubscribed = true;
    }

    private static void HandleSkillCastStarted(SkillCastVisualEvent evt)
    {
        if (evt.Caster == null) return;

        // Clean up any stale visuals for this caster first to avoid duplication
        RemoveVisuals(evt.Caster);

        // In this phase, instant skills (duration <= 0.05f) don't draw any caster rings or pulses.
        if (evt.CastDuration > 0.05f)
        {
            CreatePersistentVisuals(evt);
        }
    }

    private static void HandleSkillCastInterrupted(SkillCastVisualEvent evt)
    {
        RemoveVisuals(evt.Caster);
    }

    private static void HandleSkillCastCompleted(SkillCastVisualEvent evt)
    {
        RemoveVisuals(evt.Caster);
    }

    private static void RemoveVisuals(Unit caster)
    {
        if (caster == null) return;

        if (_activeVisuals.TryGetValue(caster, out ActiveCastVisuals visuals))
        {
            if (visuals.CasterRing != null) Destroy(visuals.CasterRing);
            if (visuals.TelegraphRing != null) Destroy(visuals.TelegraphRing);
            _activeVisuals.Remove(caster);
        }
    }

    private static Material GetSharedMaterial()
    {
        if (_sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                _sharedMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }
        return _sharedMaterial;
    }

    private static void CreatePersistentVisuals(SkillCastVisualEvent evt)
    {
        GameObject casterRingObj = new GameObject("CasterRing_Placeholder");
        CombatVfxHierarchyHelper.ParentToUnitVisual(casterRingObj, evt.Caster, keepWorldPosition: true);
        GameObject telegraphRingObj = null;

        string sortingLayerName = "Gameplay";
        int sortingOrder = 1000;
        SpriteRenderer sr = evt.Caster.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sortingLayerName = sr.sortingLayerName;
            sortingOrder = sr.sortingOrder + 8; // Draw slightly in front of unit feet
        }

        // 1. Caster Ring (subtle blue circle at feet, shrinking concéntricamente)
        LineRenderer lrCaster = casterRingObj.AddComponent<LineRenderer>();
        lrCaster.sharedMaterial = GetSharedMaterial();
        lrCaster.useWorldSpace = true;
        lrCaster.alignment = LineAlignment.View;
        lrCaster.loop = true;
        lrCaster.startWidth = 0.04f;
        lrCaster.endWidth = 0.04f;
        lrCaster.startColor = new Color(0.25f, 0.75f, 1f, 0.6f); // Cool blue cast aura, moderate alpha
        lrCaster.endColor = new Color(0.25f, 0.75f, 1f, 0.6f);
        lrCaster.sortingLayerName = sortingLayerName;
        lrCaster.sortingOrder = sortingOrder;

        CasterRingBehavior casterBehavior = casterRingObj.AddComponent<CasterRingBehavior>();
        casterBehavior.Initialize(lrCaster, evt.Caster, evt.CastDuration);

        // 2. Telegraph Ring (danger zone pulsing red)
        if (evt.HasImpactPosition)
        {
            telegraphRingObj = new GameObject("TelegraphRing_Placeholder");
            CombatVfxHierarchyHelper.ParentToCombatVfxRoot(telegraphRingObj);
            LineRenderer lrTelegraph = telegraphRingObj.AddComponent<LineRenderer>();
            lrTelegraph.sharedMaterial = GetSharedMaterial();
            lrTelegraph.useWorldSpace = true;
            lrTelegraph.alignment = LineAlignment.View;
            lrTelegraph.loop = true;
            lrTelegraph.startWidth = 0.06f;
            lrTelegraph.endWidth = 0.06f;

            Color warningColor = new Color(1f, 0.2f, 0.2f, 0.6f); // Red warning area
            lrTelegraph.startColor = warningColor;
            lrTelegraph.endColor = warningColor;
            lrTelegraph.sortingLayerName = sortingLayerName;
            lrTelegraph.sortingOrder = sortingOrder - 1; // Slightly behind the caster ring

            float radius = evt.Skill != null ? Mathf.Max(0.5f, evt.Skill.RadiusInCells) : 0.5f;

            TelegraphRingBehavior telegraphBehavior = telegraphRingObj.AddComponent<TelegraphRingBehavior>();
            telegraphBehavior.Initialize(lrTelegraph, evt.ImpactPosition, radius, warningColor);
        }

        _activeVisuals[evt.Caster] = new ActiveCastVisuals
        {
            CasterRing = casterRingObj,
            TelegraphRing = telegraphRingObj
        };
    }

    public static Vector3 ResolveUnitGroundPosition(Unit unit)
    {
        if (unit == null)
            return Vector3.zero;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
        {
            return new Vector3(combinedBounds.center.x, combinedBounds.min.y, unit.transform.position.z);
        }

        return unit.transform.position;
    }

    // Helper behaviors inside the same file for encapsulation
    private class CasterRingBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Unit _caster;
        private float _duration;
        private float _elapsed;

        public void Initialize(LineRenderer lr, Unit caster, float duration)
        {
            _lr = lr;
            _caster = caster;
            _duration = duration;
        }

        private void Update()
        {
            if (_caster == null || !_caster.gameObject.activeInHierarchy || !_caster.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            float radius = Mathf.Lerp(0.4f, 0.25f, progress); // Subtle shrinking ring (0.4f to 0.25f)

            Vector3 center = ResolveUnitGroundPosition(_caster);
            DrawRing(center, radius);
        }

        private void DrawRing(Vector3 center, float radius)
        {
            if (_lr == null) return;
            const int segments = 16;
            _lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lr.SetPosition(i, center + new Vector3(x, y, 0f));
            }
        }
    }

    private class TelegraphRingBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _radius;
        private float _elapsed;
        private Color _baseColor;

        public void Initialize(LineRenderer lr, Vector3 center, float radius, Color color)
        {
            _lr = lr;
            transform.position = center;
            _radius = radius;
            _baseColor = color;
            DrawRing();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            
            // Pulse alpha between 0.3 and 0.8
            float pulse = 0.55f + Mathf.Sin(_elapsed * 12f) * 0.25f;
            Color sc = _baseColor;
            sc.a = pulse;
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void DrawRing()
        {
            if (_lr == null) return;
            const int segments = 24;
            _lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * _radius;
                float y = Mathf.Sin(angle) * _radius;
                _lr.SetPosition(i, transform.position + new Vector3(x, y, 0f));
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

public class StatusLoopPlaceholderPresenter : MonoBehaviour
{
    [System.Serializable]
    public struct StunLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public float markSize;
        public int markCount;
        public float rotationSpeed;
        public float pulseSpeed;
        public Color color;
        public int sortingOffset;

        public void Sanitize()
        {
            if (radiusHeightFactor < 0f) radiusHeightFactor = 0.22f;
            if (radiusClamp.x < 0f) radiusClamp.x = 0.12f;
            if (radiusClamp.y < radiusClamp.x) radiusClamp.y = radiusClamp.x;
            if (markSize < 0f) markSize = 0.055f;
            if (markCount < 0) markCount = 3;
            if (rotationSpeed < 0f) rotationSpeed = 160f;
            if (pulseSpeed < 0f) pulseSpeed = 4f;
        }
    }

    [System.Serializable]
    public struct SlowLoopTuning
    {
        public float radiusHeightFactor;
        public Vector2 radiusClamp;
        public float verticalOffsetHeightFactor;
        public Vector2 verticalOffsetClamp;
        public float footCenterHeightFactor;
        public Vector2 footCenterOffsetClamp;
        public int arcCount;
        public float arcCoverage;
        public float lineWidth;
        public float pulseSpeed;
        public float contractionAmount;
        public Color color;
        public int sortingOffset;

        public void Sanitize()
        {
            if (arcCount < 1) arcCount = 1;
            if (radiusClamp.y < radiusClamp.x)
            {
                float temp = radiusClamp.x;
                radiusClamp.x = radiusClamp.y;
                radiusClamp.y = temp;
            }
            if (lineWidth <= 0f) lineWidth = 0.035f;
            if (pulseSpeed < 0f) pulseSpeed = 0f;
            if (arcCoverage < 20f) arcCoverage = 20f;
            if (arcCoverage > 360f) arcCoverage = 360f;
            if (footCenterOffsetClamp.y < footCenterOffsetClamp.x)
            {
                float temp = footCenterOffsetClamp.x;
                footCenterOffsetClamp.x = footCenterOffsetClamp.y;
                footCenterOffsetClamp.y = temp;
            }
            color.a = Mathf.Clamp01(color.a);
        }
    }

    [Header("Stun Loop Tuning")]
    [SerializeField] private float radiusHeightFactor = 0.22f;
    [SerializeField] private Vector2 radiusClamp = new Vector2(0.12f, 0.28f);
    [SerializeField] private float verticalOffsetHeightFactor = -0.12f;
    [SerializeField] private Vector2 verticalOffsetClamp = new Vector2(-0.16f, 0.06f);
    [SerializeField] private float markSize = 0.055f;
    [SerializeField] private int markCount = 3;
    [SerializeField] private float rotationSpeed = 160f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private Color color = new Color(1.0f, 0.85f, 0.18f, 0.85f);
    [SerializeField] private int sortingOffset = 28;

    [Header("Slow Loop Tuning")]
    [SerializeField] private float slowRadiusHeightFactor = 0.24f;
    [SerializeField] private Vector2 slowRadiusClamp = new Vector2(0.14f, 0.30f);
    [SerializeField] private float slowVerticalOffsetHeightFactor = 0.10f;
    [SerializeField] private Vector2 slowVerticalOffsetClamp = new Vector2(0.04f, 0.14f);
    [SerializeField] private float slowFootCenterHeightFactor = 0.18f;
    [SerializeField] private Vector2 slowFootCenterOffsetClamp = new Vector2(0.12f, 0.26f);
    [SerializeField] private int slowArcCount = 3;
    [SerializeField] private float slowArcCoverage = 220f;
    [SerializeField] private float slowLineWidth = 0.035f;
    [SerializeField] private float slowPulseSpeed = 1.4f;
    [SerializeField] private float slowContractionAmount = 0.18f;
    [SerializeField] private Color slowColor = new Color(0.35f, 0.65f, 1.0f, 0.65f);
    [SerializeField] private int slowSortingOffset = 18;
    [SerializeField] private bool enableDebugLogs = true;

    private static Material _sharedMaterial;
    
    private readonly Dictionary<(Unit, SkillEffectKind), GameObject> _activeLoops = new Dictionary<(Unit, SkillEffectKind), GameObject>();

    private void OnEnable()
    {
        StatusEffectController.AnyStatusApplied += HandleStatusApplied;
        StatusEffectController.AnyStatusRemoved += HandleStatusRemoved;
    }

    private void OnDisable()
    {
        StatusEffectController.AnyStatusApplied -= HandleStatusApplied;
        StatusEffectController.AnyStatusRemoved -= HandleStatusRemoved;
        ClearAllLoops();
    }

    public StunLoopTuning GetTuningSnapshot()
    {
        StunLoopTuning snapshot = new StunLoopTuning
        {
            radiusHeightFactor = this.radiusHeightFactor,
            radiusClamp = this.radiusClamp,
            verticalOffsetHeightFactor = this.verticalOffsetHeightFactor,
            verticalOffsetClamp = this.verticalOffsetClamp,
            markSize = this.markSize,
            markCount = this.markCount,
            rotationSpeed = this.rotationSpeed,
            pulseSpeed = this.pulseSpeed,
            color = this.color,
            sortingOffset = this.sortingOffset
        };
        snapshot.Sanitize();
        return snapshot;
    }

    public SlowLoopTuning GetSlowTuningSnapshot()
    {
        SlowLoopTuning snapshot = new SlowLoopTuning
        {
            radiusHeightFactor = this.slowRadiusHeightFactor,
            radiusClamp = this.slowRadiusClamp,
            verticalOffsetHeightFactor = this.slowVerticalOffsetHeightFactor,
            verticalOffsetClamp = this.slowVerticalOffsetClamp,
            footCenterHeightFactor = this.slowFootCenterHeightFactor,
            footCenterOffsetClamp = this.slowFootCenterOffsetClamp,
            arcCount = this.slowArcCount,
            arcCoverage = this.slowArcCoverage,
            lineWidth = this.slowLineWidth,
            pulseSpeed = this.slowPulseSpeed,
            contractionAmount = this.slowContractionAmount,
            color = this.slowColor,
            sortingOffset = this.slowSortingOffset
        };
        snapshot.Sanitize();
        return snapshot;
    }

    private void ClearAllLoops()
    {
        foreach (var kvp in _activeLoops)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        _activeLoops.Clear();
    }

    private void SweepStaleLoops()
    {
        List<(Unit, SkillEffectKind)> keysToRemove = null;
        foreach (var kvp in _activeLoops)
        {
            if (kvp.Key.Item1 == null || kvp.Value == null)
            {
                if (keysToRemove == null) keysToRemove = new List<(Unit, SkillEffectKind)>();
                keysToRemove.Add(kvp.Key);
            }
        }

        if (keysToRemove != null)
        {
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _activeLoops.Remove(keysToRemove[i]);
            }
        }
    }

    private void HandleStatusApplied(Unit unit, ActiveStatusEffect effect)
    {
        if (unit == null || effect == null || effect.Definition == null) return;

        SkillEffectKind effectType = effect.Definition.EffectType;
        if (effectType != SkillEffectKind.Stun && effectType != SkillEffectKind.Slow) return;

        CreateVisualLoopInstance(unit, effectType);
    }

    private void HandleStatusRemoved(Unit unit, ActiveStatusEffect effect)
    {
        if (unit == null || effect == null || effect.Definition == null) return;

        SweepStaleLoops();

        SkillEffectKind effectType = effect.Definition.EffectType;
        if (effectType != SkillEffectKind.Stun && effectType != SkillEffectKind.Slow) return;

        var key = (unit, effectType);
        if (_activeLoops.TryGetValue(key, out GameObject visualObj))
        {
            if (visualObj != null)
            {
                Destroy(visualObj);
            }
            _activeLoops.Remove(key);
        }
    }

    // Static fallback resolver called by debug paths
    public static GameObject CreateVisualLoop(Unit unit, SkillEffectKind effectType)
    {
        StatusLoopPlaceholderPresenter presenter = FindAnyObjectByType<StatusLoopPlaceholderPresenter>();
        if (presenter != null && presenter.isActiveAndEnabled)
        {
            return presenter.CreateVisualLoopInstance(unit, effectType);
        }
        return null;
    }

    public GameObject CreateVisualLoopInstance(Unit unit, SkillEffectKind effectType)
    {
        if (unit == null) return null;

        SweepStaleLoops();

        var key = (unit, effectType);
        if (_activeLoops.TryGetValue(key, out GameObject existingObj))
        {
            if (existingObj != null) return existingObj; // Reuse existing
            _activeLoops.Remove(key);
        }

        GameObject visualObj = null;
        if (effectType == SkillEffectKind.Stun)
        {
            visualObj = new GameObject("VFX_StatusLoop_Stun");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            StunLoopBehavior behavior = visualObj.AddComponent<StunLoopBehavior>();
            behavior.Initialize(unit, GetSharedMaterial(), GetTuningSnapshot());
        }
        else if (effectType == SkillEffectKind.Slow)
        {
            visualObj = new GameObject("VFX_StatusLoop_Slow");
            CombatVfxHierarchyHelper.ParentToUnitVisual(visualObj, unit, keepWorldPosition: true);
            SlowLoopBehavior behavior = visualObj.AddComponent<SlowLoopBehavior>();
            behavior.Initialize(unit, GetSharedMaterial(), GetSlowTuningSnapshot(), enableDebugLogs);
        }

        if (visualObj != null)
        {
            _activeLoops[key] = visualObj;
        }

        return visualObj;
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

    private static Vector3 ResolveUnitBodyPosition(Unit unit)
    {
        if (unit == null)
            return Vector3.zero;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.sprite == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
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
            return new Vector3(combinedBounds.center.x, combinedBounds.center.y, unit.transform.position.z);
        }

        return unit.transform.position;
    }

    public static Vector3 ResolveUnitHeadPosition(Unit unit, float yOffset = 0.15f)
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
            return new Vector3(combinedBounds.center.x, combinedBounds.max.y + yOffset, unit.transform.position.z);
        }

        return unit.transform.position + new Vector3(0f, 1f, 0f);
    }

    private class StunLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private StunLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _markLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;

        public void Initialize(Unit unit, Material mat, StunLoopTuning tuning)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;

            ResolveAnchor(unit);
            CreateMarks();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            _resolvedAnchorTransform = FindDescendantByName(root, "HeadStatusAnchor") ??
                                       FindDescendantByName(root, "StatusAnchor") ??
                                       FindDescendantByName(root, "BodyImpactAnchor") ??
                                       FindDescendantByName(root, "VisualAnchor");
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateMarks()
        {
            foreach (var lr in _markLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _markLrs.Clear();

            int count = Mathf.Max(0, _tuning.markCount);
            for (int i = 0; i < count; i++)
            {
                GameObject markObj = new GameObject($"StunMark_{i}");
                markObj.transform.SetParent(transform, false);
                LineRenderer lr = markObj.AddComponent<LineRenderer>();

                lr.sharedMaterial = _sharedMat;
                lr.useWorldSpace = true;
                lr.alignment = LineAlignment.View;
                lr.loop = true;
                lr.startWidth = _tuning.markSize * 0.25f;
                lr.endWidth = _tuning.markSize * 0.25f;

                ConfigureSorting(lr, _tuning.sortingOffset);
                _markLrs.Add(lr);
            }
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Stun))
            {
                Destroy(gameObject);
                return;
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 basePos;
            if (_resolvedAnchorTransform != null && _resolvedAnchorTransform.gameObject.activeInHierarchy)
            {
                basePos = _resolvedAnchorTransform.position;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds bounds))
            {
                basePos = new Vector3(bounds.center.x, bounds.max.y, _unit.transform.position.z);
            }
            else
            {
                basePos = _unit.transform.position;
            }

            float unitHeight = 1.0f;
            if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
            }

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);
            float verticalOffset = Mathf.Clamp(unitHeight * _tuning.verticalOffsetHeightFactor, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);

            Vector3 orbitCenter = basePos + new Vector3(0f, verticalOffset, 0f);

            float baseAngle = Time.time * (_tuning.rotationSpeed * Mathf.Deg2Rad);
            float tiltFactor = 0.25f;

            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * _tuning.pulseSpeed);
            float currentMarkSize = _tuning.markSize * pulse;

            Color finalColor = _tuning.color;
            float alphaPulse = 0.7f + 0.3f * Mathf.Sin(Time.time * _tuning.pulseSpeed);
            finalColor.a *= alphaPulse;

            int count = _markLrs.Count;
            for (int i = 0; i < count; i++)
            {
                LineRenderer lr = _markLrs[i];
                if (lr == null) continue;

                float angle = baseAngle + (i * 2f * Mathf.PI / count);
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius * tiltFactor;

                Vector3 markCenter = orbitCenter + new Vector3(x, y, 0f);

                lr.startColor = finalColor;
                lr.endColor = finalColor;
                lr.startWidth = currentMarkSize * 0.25f;
                lr.endWidth = currentMarkSize * 0.25f;

                DrawStar(lr, markCenter, currentMarkSize);
            }
        }

        private void DrawStar(LineRenderer lr, Vector3 center, float size)
        {
            if (lr == null) return;
            const int points = 5;
            lr.positionCount = points;

            int[] starOrder = { 0, 2, 4, 1, 3 };

            for (int i = 0; i < points; i++)
            {
                float localAngle = (starOrder[i] * 2f * Mathf.PI / points);
                float angle = localAngle + Time.time * 2f;
                float x = Mathf.Cos(angle) * size;
                float y = Mathf.Sin(angle) * size;
                lr.SetPosition(i, center + new Vector3(x, y, 0f));
            }
        }
    }

    private class SlowLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private SlowLoopTuning _tuning;
        private Material _sharedMat;
        private readonly List<LineRenderer> _backArcLrs = new List<LineRenderer>();
        private readonly List<LineRenderer> _frontArcLrs = new List<LineRenderer>();
        private Transform _resolvedAnchorTransform;
        private Vector3 _resolvedBoundsPosition;
        private bool _useBoundsPosition;
        private string _anchorSource = "None";
        private string _boundsInfo = "";
        private bool _debugLogs;
        private bool _hasLoggedDebug;

        public void Initialize(Unit unit, Material mat, SlowLoopTuning tuning, bool debugLogs)
        {
            _unit = unit;
            _sharedMat = mat;
            _tuning = tuning;
            _debugLogs = debugLogs;
            _hasLoggedDebug = false;

            ResolveAnchor(unit);
            CreateArcs();
            UpdatePositionAndVfx();
        }

        private void ResolveAnchor(Unit unit)
        {
            Transform root = unit.transform;
            Transform anchorT = FindDescendantByName(root, "FootStatusAnchor") ??
                                FindDescendantByName(root, "FeetAnchor") ??
                                FindDescendantByName(root, "BodyImpactAnchor") ??
                                FindDescendantByName(root, "BodyAnchor");

            if (anchorT != null)
            {
                _resolvedAnchorTransform = anchorT;
                _anchorSource = anchorT.name;
                return;
            }

            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(unit, out Bounds bodyBounds))
            {
                float height = bodyBounds.size.y;
                float footOffset = Mathf.Clamp(height * _tuning.footCenterHeightFactor, _tuning.footCenterOffsetClamp.x, _tuning.footCenterOffsetClamp.y);
                _resolvedBoundsPosition = new Vector3(bodyBounds.center.x, bodyBounds.min.y + footOffset, unit.transform.position.z);
                _useBoundsPosition = true;
                _anchorSource = "BodyVisualBoundsBottomCenterWithFootOffset";
                _boundsInfo = $"Min: {bodyBounds.min}, Center: {bodyBounds.center}, Max: {bodyBounds.max}, FootOffset: {footOffset}";
                return;
            }

            anchorT = FindDescendantByName(root, "GroundStatusAnchor") ??
                      FindDescendantByName(root, "GroundAnchor");

            if (anchorT != null)
            {
                _resolvedAnchorTransform = anchorT;
                _anchorSource = anchorT.name;
                return;
            }

            if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(unit, out Bounds visualBounds))
            {
                float height = visualBounds.size.y;
                float footOffset = Mathf.Clamp(height * _tuning.footCenterHeightFactor, _tuning.footCenterOffsetClamp.x, _tuning.footCenterOffsetClamp.y);
                _resolvedBoundsPosition = new Vector3(visualBounds.center.x, visualBounds.min.y + footOffset, unit.transform.position.z);
                _useBoundsPosition = true;
                _anchorSource = "UnitVisualBoundsGroundPositionWithFootOffset";
                _boundsInfo = $"Min: {visualBounds.min}, Center: {visualBounds.center}, Max: {visualBounds.max}, FootOffset: {footOffset}";
                return;
            }

            _resolvedBoundsPosition = unit.transform.position;
            _useBoundsPosition = true;
            _anchorSource = "UnitTransformPosition";
        }

        private Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform found = FindDescendantByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void CreateArcs()
        {
            foreach (var lr in _backArcLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _backArcLrs.Clear();

            foreach (var lr in _frontArcLrs)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _frontArcLrs.Clear();

            int count = Mathf.Max(1, _tuning.arcCount);
            for (int i = 0; i < count; i++)
            {
                GameObject backObj = new GameObject($"SlowArcBack_{i}");
                backObj.transform.SetParent(transform, false);
                LineRenderer backLr = backObj.AddComponent<LineRenderer>();
                SetupLr(backLr, _tuning.lineWidth);
                ConfigureSorting(backLr, -_tuning.sortingOffset);
                _backArcLrs.Add(backLr);

                GameObject frontObj = new GameObject($"SlowArcFront_{i}");
                frontObj.transform.SetParent(transform, false);
                LineRenderer frontLr = frontObj.AddComponent<LineRenderer>();
                SetupLr(frontLr, _tuning.lineWidth);
                ConfigureSorting(frontLr, _tuning.sortingOffset);
                _frontArcLrs.Add(frontLr);
            }
        }

        private void SetupLr(LineRenderer lr, float width)
        {
            lr.sharedMaterial = _sharedMat;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
        }

        private void ConfigureSorting(LineRenderer lr, int orderOffset)
        {
            if (lr == null || _unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = _unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            if (_unit == null || !_unit.gameObject.activeInHierarchy || !_unit.IsAlive || _unit.LifecycleState != UnitLifecycleState.Alive)
            {
                Destroy(gameObject);
                return;
            }

            if (_unit.StatusEffects == null || !_unit.StatusEffects.HasEffect(SkillEffectKind.Slow))
            {
                Destroy(gameObject);
                return;
            }

            CombatVfxHierarchyHelper.CounteractScale(gameObject, _unit.transform);
            UpdatePositionAndVfx();
        }

        private void UpdatePositionAndVfx()
        {
            if (_unit == null) return;

            Vector3 basePos = _resolvedAnchorTransform != null ? _resolvedAnchorTransform.position : _resolvedBoundsPosition;

            float unitHeight = 1.0f;
            if (UnitVisualBoundsUtility.TryResolveUnitBodyVisualBounds(_unit, out Bounds boundsForHeight))
            {
                unitHeight = boundsForHeight.size.y;
            }
            else if (UnitVisualBoundsUtility.TryResolveUnitVisualBounds(_unit, out Bounds normalBounds))
            {
                unitHeight = normalBounds.size.y;
            }

            float radius = Mathf.Clamp(unitHeight * _tuning.radiusHeightFactor, _tuning.radiusClamp.x, _tuning.radiusClamp.y);
            float verticalOffset = Mathf.Clamp(unitHeight * _tuning.verticalOffsetHeightFactor, _tuning.verticalOffsetClamp.x, _tuning.verticalOffsetClamp.y);

            Vector3 groundCenter = basePos + new Vector3(0f, verticalOffset, 0f);
            transform.position = groundCenter;

            if (!_hasLoggedDebug)
            {
                _hasLoggedDebug = true;
                if (_debugLogs)
                {
                    string boundsMsg = _useBoundsPosition ? $", Bounds Info: {_boundsInfo}" : "";
                    Debug.Log($"[SlowLoopVFX Debug] Unit: {_unit.name}, Anchor Source: {_anchorSource}, Resolved Base Pos: {basePos}, Final Visual Pos: {groundCenter}, Unit Height: {unitHeight}{boundsMsg}");
                }
            }

            float baseAngle = Time.time * (20f * Mathf.Deg2Rad);
            float tiltFactor = 0.35f;

            float pulseSin = Mathf.Sin(Time.time * _tuning.pulseSpeed);
            float contractionVal = (pulseSin + 1f) * 0.5f;
            float scaleFactor = 1f - (_tuning.contractionAmount * contractionVal);
            float currentRadius = radius * scaleFactor;

            Color finalColor = _tuning.color;
            float alphaPulse = 0.75f + 0.25f * Mathf.Sin(Time.time * _tuning.pulseSpeed);
            finalColor.a *= alphaPulse;

            int count = _tuning.arcCount;
            float coverageRad = _tuning.arcCoverage * Mathf.Deg2Rad;
            float arcLenRad = coverageRad / count;
            float spacingRad = (2f * Mathf.PI - coverageRad) / count;

            for (int i = 0; i < count; i++)
            {
                LineRenderer backLr = _backArcLrs[i];
                LineRenderer frontLr = _frontArcLrs[i];
                if (backLr == null || frontLr == null) continue;

                backLr.startColor = finalColor;
                backLr.endColor = finalColor;
                backLr.startWidth = _tuning.lineWidth;
                backLr.endWidth = _tuning.lineWidth;

                frontLr.startColor = finalColor;
                frontLr.endColor = finalColor;
                frontLr.startWidth = _tuning.lineWidth;
                frontLr.endWidth = _tuning.lineWidth;

                float startAngle = baseAngle + i * (arcLenRad + spacingRad);
                float endAngle = startAngle + arcLenRad;

                float frictionWiggle = 0.012f * radius;

                DrawSplitArc(backLr, frontLr, groundCenter, currentRadius, startAngle, endAngle, tiltFactor, frictionWiggle);
            }
        }

        private void DrawSplitArc(LineRenderer backLr, LineRenderer frontLr, Vector3 center, float radius, float startAngleRad, float endAngleRad, float tiltFactor, float frictionWiggle)
        {
            const int points = 16;
            List<Vector3> backPoints = new List<Vector3>();
            List<Vector3> frontPoints = new List<Vector3>();

            Vector3 previousPoint = Vector3.zero;
            bool wasPreviousBack = false;

            for (int j = 0; j < points; j++)
            {
                float t = (float)j / (points - 1);
                float angle = Mathf.Lerp(startAngleRad, endAngleRad, t);

                float rumble = frictionWiggle * Mathf.Sin(angle * 12f + Time.time * 4f);
                float r = radius + rumble;

                float x = Mathf.Cos(angle) * r;
                float y = Mathf.Sin(angle) * r * tiltFactor;
                Vector3 worldPoint = center + new Vector3(x, y, 0f);

                bool isBack = y > 0f;

                if (isBack)
                {
                    backPoints.Add(worldPoint);
                    if (j > 0 && !wasPreviousBack)
                    {
                        backPoints.Insert(backPoints.Count - 1, previousPoint);
                    }
                }
                else
                {
                    frontPoints.Add(worldPoint);
                    if (j > 0 && wasPreviousBack)
                    {
                        frontPoints.Insert(frontPoints.Count - 1, previousPoint);
                    }
                }

                previousPoint = worldPoint;
                wasPreviousBack = isBack;
            }

            backLr.positionCount = backPoints.Count;
            for (int k = 0; k < backPoints.Count; k++)
            {
                backLr.SetPosition(k, backPoints[k]);
            }

            frontLr.positionCount = frontPoints.Count;
            for (int k = 0; k < frontPoints.Count; k++)
            {
                frontLr.SetPosition(k, frontPoints[k]);
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

public class StatusLoopPlaceholderPresenter : MonoBehaviour
{
    private static Material _sharedMaterial;
    private static bool _isSubscribed;
    
    private static readonly Dictionary<(Unit, SkillEffectKind), GameObject> _activeLoops = new Dictionary<(Unit, SkillEffectKind), GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_isSubscribed) return;
        StatusEffectController.AnyStatusApplied += HandleStatusApplied;
        StatusEffectController.AnyStatusRemoved += HandleStatusRemoved;
        _isSubscribed = true;
    }

    private static void SweepStaleLoops()
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

    private static void HandleStatusApplied(Unit unit, ActiveStatusEffect effect)
    {
        if (unit == null || effect == null || effect.Definition == null) return;

        SweepStaleLoops();

        SkillEffectKind effectType = effect.Definition.EffectType;
        if (effectType != SkillEffectKind.Stun && effectType != SkillEffectKind.Slow) return;

        var key = (unit, effectType);
        if (_activeLoops.TryGetValue(key, out GameObject existingObj))
        {
            if (existingObj != null) return; // Already exists, ignore/reuse
            _activeLoops.Remove(key); // Stale reference, clean up
        }

        GameObject visualObj = CreateVisualLoop(unit, effectType);
        if (visualObj != null)
        {
            _activeLoops[key] = visualObj;
        }
    }

    private static void HandleStatusRemoved(Unit unit, ActiveStatusEffect effect)
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

    private static GameObject CreateVisualLoop(Unit unit, SkillEffectKind effectType)
    {
        if (effectType == SkillEffectKind.Stun)
        {
            GameObject obj = new GameObject("VFX_StatusLoop_Stun");
            CombatVfxHierarchyHelper.ParentToUnitVisual(obj, unit, keepWorldPosition: true);
            StunLoopBehavior behavior = obj.AddComponent<StunLoopBehavior>();
            behavior.Initialize(unit, GetSharedMaterial());
            return obj;
        }
        else if (effectType == SkillEffectKind.Slow)
        {
            GameObject obj = new GameObject("VFX_StatusLoop_Slow");
            CombatVfxHierarchyHelper.ParentToUnitVisual(obj, unit, keepWorldPosition: true);
            SlowLoopBehavior behavior = obj.AddComponent<SlowLoopBehavior>();
            behavior.Initialize(unit, GetSharedMaterial());
            return obj;
        }

        return null;
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
        private LineRenderer _lr;

        public void Initialize(Unit unit, Material mat)
        {
            _unit = unit;

            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.sharedMaterial = mat;
            _lr.useWorldSpace = true;
            _lr.alignment = LineAlignment.View;
            _lr.loop = true;
            _lr.startWidth = 0.035f;
            _lr.endWidth = 0.035f;

            Color gold = new Color(1.0f, 0.82f, 0.15f, 0.55f);
            _lr.startColor = gold;
            _lr.endColor = gold;

            ConfigureSorting(unit, 12);
            UpdatePosition();
        }

        private void ConfigureSorting(Unit unit, int orderOffset)
        {
            if (_lr == null || unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            _lr.sortingLayerName = sortingLayerName;
            _lr.sortingOrder = sortingOrder;
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
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector3 headPos = ResolveUnitHeadPosition(_unit, 0.15f);
            DrawHalo(headPos);
        }

        private void DrawHalo(Vector3 center)
        {
            if (_lr == null) return;
            const int segments = 16;
            _lr.positionCount = segments;
            float radius = 0.22f;
            float tiltFactor = 0.28f;
            float rotOffset = Time.time * 4f;

            for (int i = 0; i < segments; i++)
            {
                float localAngle = ((float)i / segments) * Mathf.PI * 2f;
                float angle = localAngle + rotOffset;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius * tiltFactor;
                _lr.SetPosition(i, center + new Vector3(x, y, 0f));
            }
        }
    }

    private class SlowLoopBehavior : MonoBehaviour
    {
        private Unit _unit;
        private LineRenderer _innerLr;
        private LineRenderer _outerLr;

        public void Initialize(Unit unit, Material mat)
        {
            _unit = unit;

            GameObject innerObj = new GameObject("InnerRing");
            innerObj.transform.SetParent(transform, false);
            _innerLr = innerObj.AddComponent<LineRenderer>();
            SetupLineRenderer(_innerLr, mat, 0.03f);

            GameObject outerObj = new GameObject("OuterRing");
            outerObj.transform.SetParent(transform, false);
            _outerLr = outerObj.AddComponent<LineRenderer>();
            SetupLineRenderer(_outerLr, mat, 0.03f);

            ConfigureSorting(unit, -1);
            UpdatePosition();
        }

        private void SetupLineRenderer(LineRenderer lr, Material mat, float width)
        {
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.loop = true;
            lr.startWidth = width;
            lr.endWidth = width;

            Color celeste = new Color(0.25f, 0.65f, 1.0f, 0.45f);
            lr.startColor = celeste;
            lr.endColor = celeste;
        }

        private void ConfigureSorting(Unit unit, int orderOffset)
        {
            if (unit == null) return;
            string sortingLayerName = "Gameplay";
            int sortingOrder = 1000;

            SpriteRenderer sr = unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }

            if (_innerLr != null)
            {
                _innerLr.sortingLayerName = sortingLayerName;
                _innerLr.sortingOrder = sortingOrder;
            }
            if (_outerLr != null)
            {
                _outerLr.sortingLayerName = sortingLayerName;
                _outerLr.sortingOrder = sortingOrder;
            }
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
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector3 groundPos = ResolveUnitGroundPosition(_unit);
            
            float innerPulse = 0.24f + 0.02f * Mathf.Sin(Time.time * 2.5f);
            DrawFlatRing(_innerLr, groundPos, innerPulse);

            float outerPulse = 0.38f + 0.03f * Mathf.Sin(Time.time * 2.5f + Mathf.PI);
            DrawFlatRing(_outerLr, groundPos, outerPulse);
        }

        private void DrawFlatRing(LineRenderer lr, Vector3 center, float radius)
        {
            if (lr == null) return;
            const int segments = 16;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                lr.SetPosition(i, center + new Vector3(x, y, 0f));
            }
        }
    }
}

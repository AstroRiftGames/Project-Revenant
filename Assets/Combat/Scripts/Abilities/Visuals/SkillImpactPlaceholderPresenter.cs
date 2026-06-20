using UnityEngine;
using System;
using System.Collections.Generic;

public class SkillImpactPlaceholderPresenter : MonoBehaviour
{
    private static Material _sharedMaterial;
    private static bool _isSubscribed;
    private static readonly bool EnableTracerLogs = false;
    private static int _particleBurstCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_isSubscribed) return;
        SkillCaster.AnySkillEffectsAppliedForVisuals += HandleSkillEffectsApplied;
        _isSubscribed = true;
    }

    private static void HandleSkillEffectsApplied(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (skill == null || context == null || impacts == null) return;

        _particleBurstCount = 0;

        // 1. Process target-based impacts (Damage, Heal, Shield, Buff, Debuff, Status, Knockback)
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null) continue;

            Vector3 targetPos = ResolveImpactPosition(impact, context);

            if (skill.CompositionEffects != null)
            {
                for (int j = 0; j < skill.CompositionEffects.Length; j++)
                {
                    SkillCompositionEffect effect = skill.CompositionEffects[j];
                    if (effect.EffectKind != SkillEffectKind.Summon)
                    {
                        CreateVisualForEffect(effect.EffectKind, impact, targetPos, context);
                    }
                }
            }
        }

        // 2. Process spatial summon impact (only once per skill resolution)
        if (skill.CompositionEffects != null)
        {
            for (int j = 0; j < skill.CompositionEffects.Length; j++)
            {
                SkillCompositionEffect effect = skill.CompositionEffects[j];
                if (effect.EffectKind == SkillEffectKind.Summon)
                {
                    CreateSummonVisual(skill, context, impacts);
                }
            }
        }

        // 3. Process origin tracer visuals (Phase 3B)
        CreateTracers(skill, context, impacts);
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

    private static Vector3 ResolveImpactPosition(SkillImpact impact, SkillContext context)
    {
        if (impact != null)
        {
            if (impact.HasTargetUnit)
            {
                return ResolveUnitGroundPosition(impact.TargetUnit);
            }
            if (impact.Kind == SkillImpactKind.AreaPoint)
            {
                return impact.WorldPosition;
            }
            if (impact.HasCell && context != null && context.RoomGrid != null)
            {
                return context.RoomGrid.CellToWorld(new Vector3Int(impact.Cell.x, impact.Cell.y, 0));
            }
        }
        if (context != null)
        {
            if (context.HasImpactCenterUnit)
            {
                return ResolveUnitGroundPosition(context.ImpactCenterUnit);
            }
            return context.ImpactCenterWorld;
        }
        return Vector3.zero;
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

    public static Vector3 ResolveUnitCenterPosition(Unit unit)
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
            return new Vector3(combinedBounds.center.x, combinedBounds.center.y, unit.transform.position.z);
        }

        return unit.transform.position;
    }

    private static void CreateTracers(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        Unit caster = context.Caster;
        if (caster == null) return;

        Vector3 origin = ResolveUnitCenterPosition(caster);
        if (origin == Vector3.zero) return;

        SkillEffectKind mainEffect = GetMainEffectKind(skill);
        Color tracerColor = GetTracerColor(mainEffect);

        int tracerCount = 0;
        const int maxTracers = 6;
        HashSet<Vector3> drawnPositions = new HashSet<Vector3>();

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null) continue;

            Vector3 destination = Vector3.zero;
            if (impact.HasTargetUnit)
            {
                if (impact.TargetUnit == caster)
                {
#if UNITY_EDITOR
                    if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer omitted: target is self-cast for unit {caster.name}.");
#endif
                    continue; // Skip self-tracer
                }
                destination = ResolveUnitCenterPosition(impact.TargetUnit);
            }
            else
            {
                destination = ResolveImpactPosition(impact, context);
            }

            if (destination == Vector3.zero) continue;
            if (drawnPositions.Contains(destination)) continue;

            // Reduce minimum distance threshold to 0.01f (about 0.1 cells) so close targets get tracers
            if ((destination - origin).sqrMagnitude < 0.01f)
            {
#if UNITY_EDITOR
                if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer omitted: distance too short between {caster.name} and {destination}.");
#endif
                continue; 
            }

            if (tracerCount >= maxTracers)
            {
#if UNITY_EDITOR
                if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer omitted: reached maximum tracer count ({maxTracers}) for skill {skill.DisplayName}.");
#endif
                break;
            }

#if UNITY_EDITOR
            if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer spawned from {caster.name} to target position {destination} for skill '{skill.DisplayName}' ({mainEffect}).");
#endif
            CreateSingleTracer(caster, origin, destination, tracerColor);
            drawnPositions.Add(destination);
            tracerCount++;
        }

        // Fallback: If no tracers drawn (e.g. AoE with no targets, or pure spatial effect) but impact center is valid
        if (tracerCount == 0 && context.ImpactCenterWorld != Vector3.zero)
        {
            if (!context.HasImpactCenterUnit || context.ImpactCenterUnit != caster)
            {
                Vector3 destination = context.ImpactCenterWorld;
                if ((destination - origin).sqrMagnitude >= 0.01f)
                {
#if UNITY_EDITOR
                    if (EnableTracerLogs) UnityEngine.Debug.Log($"[SkillImpactPlaceholderPresenter] Tracer fallback spawned from {caster.name} to impact center {destination} for skill '{skill.DisplayName}' ({mainEffect}).");
#endif
                    CreateSingleTracer(caster, origin, destination, tracerColor);
                }
            }
        }
    }

    private static void CreateSingleTracer(Unit caster, Vector3 origin, Vector3 destination, Color color)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Tracer");
        CombatVfxHierarchyHelper.ParentToCombatVfxRoot(obj);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = false;
        
        // Increased width for visibility: start 0.065f, end 0.03f
        lr.startWidth = 0.065f;
        lr.endWidth = 0.03f;

        lr.startColor = color;
        lr.endColor = color;
        
        // High sorting offset (+45) to draw on top of units and standard impact VFX
        ConfigureSorting(obj, caster, 45); 

        var behavior = obj.AddComponent<TracerVisualBehavior>();
        // Increased duration to 0.35s for validation in play mode
        behavior.Initialize(lr, origin, destination, 0.35f, color);
    }

    private static SkillEffectKind GetMainEffectKind(SkillData skill)
    {
        if (skill == null || skill.CompositionEffects == null || skill.CompositionEffects.Length == 0)
        {
            return SkillEffectKind.Damage;
        }

        for (int i = 0; i < skill.CompositionEffects.Length; i++)
        {
            var kind = skill.CompositionEffects[i].EffectKind;
            if (kind == SkillEffectKind.Damage) return kind;
        }
        for (int i = 0; i < skill.CompositionEffects.Length; i++)
        {
            var kind = skill.CompositionEffects[i].EffectKind;
            if (kind == SkillEffectKind.Heal || kind == SkillEffectKind.Shield) return kind;
        }

        return skill.CompositionEffects[0].EffectKind;
    }

    private static Color GetTracerColor(SkillEffectKind effectKind)
    {
        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                return new Color(1f, 0.4f, 0.1f, 1f); // Bright red-orange

            case SkillEffectKind.Heal:
                return new Color(0.2f, 1f, 0.4f, 1f); // Neon green

            case SkillEffectKind.Shield:
                return new Color(0.3f, 0.85f, 1f, 1f); // Sky blue

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                return new Color(1f, 0.9f, 0.4f, 1f); // Gold

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                return new Color(0.9f, 0.2f, 0.9f, 1f); // Hot pink/magenta

            case SkillEffectKind.Summon:
                return new Color(0.1f, 1f, 1f, 1f); // Cyan

            default:
                return new Color(0.8f, 0.8f, 0.8f, 1f); // Gray
        }
    }

    private static void ConfigureSorting(GameObject obj, Unit targetUnit, int orderOffset)
    {
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        if (lr == null) return;

        string sortingLayerName = "Gameplay";
        int sortingOrder = 1000;

        if (targetUnit != null)
        {
            SpriteRenderer sr = targetUnit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sortingLayerName = sr.sortingLayerName;
                sortingOrder = sr.sortingOrder + orderOffset;
            }
        }

        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;
    }

    private static void CreateVisualForEffect(SkillEffectKind effectKind, SkillImpact impact, Vector3 targetPos, SkillContext context)
    {
        Unit targetUnit = impact.HasTargetUnit ? impact.TargetUnit : null;

        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                CreateDamageImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Heal:
                CreateHealImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Shield:
                CreateShieldImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                CreateBuffImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
                CreateStatusImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                CreateDebuffImpact(targetUnit, targetPos);
                break;

            case SkillEffectKind.Knockback:
                CreateKnockbackImpact(targetUnit, targetPos);
                break;
        }

        // Phase 3C: Burst of particles
        CreateImpactParticles(targetPos, effectKind, targetUnit, context);
    }

    // 1. Damage: pequeño burst/anillo rojo-naranja en target
    private static void CreateDamageImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Damage");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        Color color = new Color(1f, 0.35f, 0.1f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Lifetime 0.35s, size 0.08f to 0.45f
        behavior.Initialize(lr, targetPos, 0.35f, 0.08f, 0.45f, 0f, 0f);
    }

    // 2. Heal: pulso verde o cruces verdes ascendentes muy simples
    private static void CreateHealImpact(Unit targetUnit, Vector3 targetPos)
    {
        // Spawn 2 green crosses with slightly randomized start positions ascending
        int count = 2;
        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject($"VFX_Placeholder_Heal_Cross_{i}");
            CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.sharedMaterial = GetSharedMaterial();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.loop = false;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;

            Color color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            lr.startColor = color;
            lr.endColor = color;
            ConfigureSorting(obj, targetUnit, 12);

            // Add slight spatial offsets relative to the target position
            Vector3 offsetPos = targetPos + new Vector3(
                UnityEngine.Random.Range(-0.2f, 0.2f),
                UnityEngine.Random.Range(0.0f, 0.25f),
                0f
            );

            var behavior = obj.AddComponent<CrossImpactBehavior>();
            // Cross size 0.12f, moves upward at speed 0.75 units/sec, lasts 0.5s
            behavior.Initialize(lr, offsetPos, 0.5f, 0.12f, 0.75f, color);
        }
    }

    // 3. Shield: anillo azul/celeste breve alrededor del target
    private static void CreateShieldImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Shield");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        Color color = new Color(0.3f, 0.75f, 1f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Animate from radius 0.32f to 0.48f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.32f, 0.48f, 0f, 0f);
    }

    // 4. Buff: pulso ascendente claro
    private static void CreateBuffImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Buff");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;

        Color color = new Color(1f, 0.85f, 0.3f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Radius 0.28f, rises from offset 0f to 0.65f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.28f, 0.32f, 0f, 0.65f);
    }

    // 5. Debuff: pulso descendente oscuro o violeta
    private static void CreateDebuffImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Debuff");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;

        Color color = new Color(0.6f, 0.2f, 0.85f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Radius 0.32f, falls from offset 0.65f to 0f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.32f, 0.28f, 0.65f, 0f);
    }

    // 6. Status: anillo breve sobre target o bajo pies, color genérico
    private static void CreateStatusImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Status");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        Color color = new Color(0.9f, 0.2f, 0.6f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Ring at feet/ground position expanding from 0.24f to 0.38f over 0.45s
        behavior.Initialize(lr, targetPos, 0.45f, 0.24f, 0.38f, 0f, 0f);
    }

    // 7. Knockback: impacto simple en target
    private static void CreateKnockbackImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Knockback");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        Color color = new Color(0.8f, 0.95f, 1f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, targetUnit, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Expand ring from 0.1f to 0.5f over 0.35s
        behavior.Initialize(lr, targetPos, 0.35f, 0.1f, 0.5f, 0f, 0f);
    }

    // 8. Summon: pulso circular en la celda de aparición
    private static void CreateSummonVisual(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        Vector3 summonPos = Vector3.zero;
        bool hasSummonPos = false;

        // Try to resolve the summon cell position from target cell or impacts
        if (context.HasTargetCell && context.RoomGrid != null)
        {
            summonPos = context.RoomGrid.CellToWorld(new Vector3Int(context.TargetCell.x, context.TargetCell.y, 0));
            hasSummonPos = true;
        }
        else if (impacts != null && impacts.Count > 0)
        {
            for (int i = 0; i < impacts.Count; i++)
            {
                var impact = impacts[i];
                if (impact != null && impact.HasCell && context.RoomGrid != null)
                {
                    summonPos = context.RoomGrid.CellToWorld(new Vector3Int(impact.Cell.x, impact.Cell.y, 0));
                    hasSummonPos = true;
                    break;
                }
            }
        }

        // Fallback to impact center if cell is not explicit but impact center is valid
        if (!hasSummonPos && context.ImpactCenterWorld != Vector3.zero)
        {
            summonPos = context.ImpactCenterWorld;
            hasSummonPos = true;
        }

        if (!hasSummonPos) return;

        GameObject obj = new GameObject("VFX_Placeholder_Summon");
        CombatVfxHierarchyHelper.ParentToCombatVfxRoot(obj);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = true;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.06f;

        Color color = new Color(0.1f, 0.95f, 0.95f, 0.9f);
        lr.startColor = color;
        lr.endColor = color;
        ConfigureSorting(obj, null, 12);

        var behavior = obj.AddComponent<RingImpactBehavior>();
        // Expand ring from 0.15f to 0.65f over 0.55s
        behavior.Initialize(lr, summonPos, 0.55f, 0.15f, 0.65f, 0f, 0f);

        // Phase 3C: Summon particles
        CreateImpactParticles(summonPos, SkillEffectKind.Summon, null, context);
    }

    // Helper components inside the same file for encapsulation
    private class RingImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _lifetime;
        private float _elapsed;
        private float _startRadius;
        private float _endRadius;
        private float _startVerticalOffset;
        private float _endVerticalOffset;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, Vector3 center, float lifetime, float startRadius, float endRadius, float startVerticalOffset, float endVerticalOffset)
        {
            _lr = lr;
            transform.position = center;
            _lifetime = lifetime;
            _startRadius = startRadius;
            _endRadius = endRadius;
            _startVerticalOffset = startVerticalOffset;
            _endVerticalOffset = endVerticalOffset;
            _startColor = lr.startColor;
            _endColor = lr.endColor;
            _endColor.a = 0f;
            DrawRing(startRadius, startVerticalOffset);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _lifetime;
            float currentRadius = Mathf.Lerp(_startRadius, _endRadius, t);
            float currentVertical = Mathf.Lerp(_startVerticalOffset, _endVerticalOffset, t);
            DrawRing(currentRadius, currentVertical);

            Color sc = _startColor;
            sc.a *= (1f - t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void DrawRing(float radius, float verticalOffset)
        {
            if (_lr == null) return;

            const int segments = 16;
            _lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lr.SetPosition(i, transform.position + new Vector3(x, y + verticalOffset, 0f));
            }
        }
    }

    private class CrossImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private float _lifetime;
        private float _elapsed;
        private float _size;
        private float _verticalSpeed;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, Vector3 center, float lifetime, float size, float verticalSpeed, Color color)
        {
            _lr = lr;
            transform.position = center;
            _lifetime = lifetime;
            _size = size;
            _verticalSpeed = verticalSpeed;
            _startColor = color;
            _endColor = color;
            _endColor.a = 0f;
            DrawCross(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _lifetime;
            float currentVerticalOffset = _verticalSpeed * _elapsed;
            DrawCross(currentVerticalOffset);

            Color sc = _startColor;
            sc.a *= (1f - t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void DrawCross(float verticalOffset)
        {
            if (_lr == null) return;

            Vector3 centerOffset = transform.position + new Vector3(0f, verticalOffset, 0f);
            
            _lr.positionCount = 5;
            _lr.SetPosition(0, centerOffset + Vector3.left * _size);
            _lr.SetPosition(1, centerOffset + Vector3.right * _size);
            _lr.SetPosition(2, centerOffset);
            _lr.SetPosition(3, centerOffset + Vector3.up * _size);
            _lr.SetPosition(4, centerOffset + Vector3.down * _size);
        }
    }

    private class TracerVisualBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _start;
        private Vector3 _end;
        private float _lifetime;
        private float _elapsed;
        private Color _color;

        public void Initialize(LineRenderer lr, Vector3 start, Vector3 end, float lifetime, Color color)
        {
            _lr = lr;
            _start = start;
            _end = end;
            _lifetime = lifetime;
            _color = color;
            
            if (_lr != null)
            {
                _lr.positionCount = 2;
                _lr.SetPosition(0, _start);
                _lr.SetPosition(1, _end);
                _lr.startColor = _color;
                _lr.endColor = _color;
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _lifetime;
            Color sc = _color;
            sc.a *= (1f - t);

            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }
    }

    private static void CreateImpactParticles(Vector3 targetPos, SkillEffectKind effectKind, Unit targetUnit, SkillContext context)
    {
        if (_particleBurstCount >= 6) return;

        // Skip self-cast particles if it is not heal or buff
        if (targetUnit != null && context != null && context.Caster != null && targetUnit == context.Caster)
        {
            bool isHealOrBuff = effectKind == SkillEffectKind.Heal ||
                               effectKind == SkillEffectKind.Buff ||
                               effectKind == SkillEffectKind.StrengthBuff ||
                               effectKind == SkillEffectKind.Haste;
            if (!isHealOrBuff) return;
        }

        _particleBurstCount++;

        // Determine base position (center or ground/feet)
        Vector3 spawnPos = targetPos;
        if (targetUnit != null)
        {
            if (effectKind == SkillEffectKind.Damage ||
                effectKind == SkillEffectKind.Shield ||
                effectKind == SkillEffectKind.Knockback)
            {
                spawnPos = ResolveUnitCenterPosition(targetUnit);
            }
            else
            {
                spawnPos = ResolveUnitGroundPosition(targetUnit);
            }
        }

        // Determine effect-specific color
        Color effectColor = Color.white;
        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                effectColor = new Color(1.0f, 0.5f, 0.15f, 1.0f); // Bright yellow-orange
                break;
            case SkillEffectKind.Heal:
                effectColor = new Color(0.2f, 1.0f, 0.4f, 1.0f); // Neon green
                break;
            case SkillEffectKind.Shield:
                effectColor = new Color(0.3f, 0.85f, 1.0f, 1.0f); // Sky blue
                break;
            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                effectColor = new Color(1.0f, 0.9f, 0.4f, 1.0f); // Bright gold
                break;
            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                effectColor = new Color(0.9f, 0.2f, 0.9f, 1.0f); // Bright magenta/violet
                break;
            case SkillEffectKind.Knockback:
                effectColor = new Color(0.85f, 0.95f, 1.0f, 1.0f); // Bright white-blue
                break;
            case SkillEffectKind.Summon:
                effectColor = new Color(0.2f, 1.0f, 1.0f, 1.0f); // Bright cyan
                break;
        }

        // Phase 3C: Spawn central flash to anchor visual impact
        CreateCentralFlash(targetUnit, spawnPos, effectColor);

        // Particle widths
        float startWidth = 0.055f;
        float endWidth = 0.022f;

        switch (effectKind)
        {
            case SkillEffectKind.Damage:
                {
                    int particleCount = UnityEngine.Random.Range(10, 13);
                    for (int i = 0; i < particleCount; i++)
                    {
                        float angle = (i * 2f * Mathf.PI) / particleCount;
                        angle += UnityEngine.Random.Range(-0.1f, 0.1f);
                        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        float speed = UnityEngine.Random.Range(2.5f, 4.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.45f, 0.55f);
                        float length = UnityEngine.Random.Range(0.1f, 0.18f);
                        float drag = 3.5f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;

            case SkillEffectKind.Heal:
                {
                    int particleCount = UnityEngine.Random.Range(6, 9);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 offsetPos = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0f, 0.2f), 0f);
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 1f, 0f).normalized;
                        float speed = UnityEngine.Random.Range(1.2f, 2.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.6f, 0.75f);
                        float length = UnityEngine.Random.Range(0.08f, 0.15f);
                        CreateSingleParticle(targetUnit, offsetPos, velocity, lifetime, length, startWidth, endWidth, effectColor);
                    }
                }
                break;

            case SkillEffectKind.Shield:
                {
                    int particleCount = UnityEngine.Random.Range(8, 11);
                    for (int i = 0; i < particleCount; i++)
                    {
                        float angle = (i * 2f * Mathf.PI) / particleCount;
                        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        float speed = UnityEngine.Random.Range(0.9f, 1.6f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.55f, 0.7f);
                        float length = 0.08f;
                        float drag = 2.0f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;

            case SkillEffectKind.Haste:
            case SkillEffectKind.StrengthBuff:
            case SkillEffectKind.Buff:
                {
                    int particleCount = UnityEngine.Random.Range(6, 9);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 offsetPos = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0f, 0.2f), 0f);
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 1f, 0f).normalized;
                        float speed = UnityEngine.Random.Range(1.5f, 2.5f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.55f, 0.7f);
                        float length = UnityEngine.Random.Range(0.08f, 0.15f);
                        CreateSingleParticle(targetUnit, offsetPos, velocity, lifetime, length, startWidth, endWidth, effectColor);
                    }
                }
                break;

            case SkillEffectKind.Slow:
            case SkillEffectKind.Stun:
            case SkillEffectKind.Taunt:
            case SkillEffectKind.Blind:
            case SkillEffectKind.PoisonBurn:
            case SkillEffectKind.Debuff:
            case SkillEffectKind.StatModifierDebuff:
                {
                    int particleCount = UnityEngine.Random.Range(6, 9);
                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 offsetPos = spawnPos + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.4f, 0.8f), 0f);
                        Vector3 dir = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), -1f, 0f).normalized;
                        float speed = UnityEngine.Random.Range(1.2f, 2.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.55f, 0.7f);
                        float length = UnityEngine.Random.Range(0.08f, 0.15f);
                        CreateSingleParticle(targetUnit, offsetPos, velocity, lifetime, length, startWidth, endWidth, effectColor);
                    }
                }
                break;

            case SkillEffectKind.Knockback:
                {
                    int particleCount = UnityEngine.Random.Range(10, 13);
                    Vector3 pushDir = Vector3.zero;
                    if (context != null && context.Caster != null)
                    {
                        Vector3 casterPos = ResolveUnitCenterPosition(context.Caster);
                        pushDir = spawnPos - casterPos;
                        pushDir.z = 0f;
                        pushDir = pushDir.normalized;
                    }

                    bool hasDirection = pushDir != Vector3.zero;
                    float centerAngle = hasDirection ? Mathf.Atan2(pushDir.y, pushDir.x) : 0f;

                    for (int i = 0; i < particleCount; i++)
                    {
                        Vector3 dir;
                        if (hasDirection)
                        {
                            float spreadAngle = centerAngle + UnityEngine.Random.Range(-Mathf.PI / 6f, Mathf.PI / 6f);
                            dir = new Vector3(Mathf.Cos(spreadAngle), Mathf.Sin(spreadAngle), 0f);
                        }
                        else
                        {
                            float angle = (i * 2f * Mathf.PI) / particleCount;
                            dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        }

                        float speed = UnityEngine.Random.Range(3.0f, 5.2f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.45f, 0.6f);
                        float length = UnityEngine.Random.Range(0.12f, 0.2f);
                        float drag = 2.5f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;

            case SkillEffectKind.Summon:
                {
                    int particleCount = 12;
                    for (int i = 0; i < particleCount; i++)
                    {
                        float angle = (i * 2f * Mathf.PI) / particleCount;
                        Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                        float speed = UnityEngine.Random.Range(2.0f, 3.3f);
                        Vector3 velocity = dir * speed;
                        float lifetime = UnityEngine.Random.Range(0.65f, 0.85f);
                        float length = UnityEngine.Random.Range(0.1f, 0.18f);
                        float drag = 2.0f;
                        CreateSingleParticle(targetUnit, spawnPos, velocity, lifetime, length, startWidth, endWidth, effectColor, drag);
                    }
                }
                break;
        }
    }

    private static void CreateCentralFlash(Unit targetUnit, Vector3 position, Color effectColor)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Flash");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = false;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;

        // White with tint of effectColor
        Color flashColor = Color.Lerp(Color.white, effectColor, 0.4f);
        flashColor.a = 1.0f; // Initial opacity
        lr.startColor = flashColor;
        lr.endColor = flashColor;

        ConfigureSorting(obj, targetUnit, 35); // Particle order offset +35

        var behavior = obj.AddComponent<CrossImpactBehavior>();
        behavior.Initialize(lr, position, 0.15f, 0.18f, 0f, flashColor);
    }

    private static void CreateSingleParticle(Unit targetUnit, Vector3 position, Vector3 velocity, float lifetime, float length, float startWidth, float endWidth, Color color, float drag = 0f, Vector3 gravity = default)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Particle");
        CombatVfxHierarchyHelper.ParentToTargetOrRoot(obj, targetUnit);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.sharedMaterial = GetSharedMaterial();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.loop = false;
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;
        lr.startColor = color;
        lr.endColor = color;

        ConfigureSorting(obj, targetUnit, 35); // Particle order offset +35

        var behavior = obj.AddComponent<ImpactParticleBehavior>();
        behavior.Initialize(lr, position, velocity, lifetime, length, color, drag, gravity);
    }

    private class ImpactParticleBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _position;
        private Vector3 _velocity;
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private Color _endColor;
        private float _drag;
        private Vector3 _gravity;
        private float _length;

        public void Initialize(LineRenderer lr, Vector3 startPos, Vector3 velocity, float lifetime, float length, Color color, float drag = 0f, Vector3 gravity = default)
        {
            _lr = lr;
            _position = startPos;
            _velocity = velocity;
            _lifetime = lifetime;
            _length = length;
            _startColor = color;
            _endColor = color;
            _endColor.a = 0f;
            _drag = drag;
            _gravity = gravity;

            UpdateVisual();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _lifetime;

            if (_drag > 0f)
            {
                _velocity -= _velocity * _drag * Time.deltaTime;
            }
            _velocity += _gravity * Time.deltaTime;

            _position += _velocity * Time.deltaTime;

            UpdateVisual();

            Color sc = Color.Lerp(_startColor, _endColor, t);
            if (_lr != null)
            {
                _lr.startColor = sc;
                _lr.endColor = sc;
            }
        }

        private void UpdateVisual()
        {
            if (_lr == null) return;

            _lr.positionCount = 2;
            _lr.SetPosition(0, _position);

            Vector3 tailDirection = _velocity.normalized;
            if (tailDirection == Vector3.zero)
            {
                tailDirection = Vector3.up;
            }
            _lr.SetPosition(1, _position - tailDirection * _length);
        }
    }
}

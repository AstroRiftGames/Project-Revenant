using UnityEngine;
using System;
using System.Collections.Generic;

public class SkillImpactPlaceholderPresenter : MonoBehaviour
{
    private static Material _sharedMaterial;
    private static bool _isSubscribed;

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
    }

    // 1. Damage: pequeño burst/anillo rojo-naranja en target
    private static void CreateDamageImpact(Unit targetUnit, Vector3 targetPos)
    {
        GameObject obj = new GameObject("VFX_Placeholder_Damage");
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
    }

    // Helper components inside the same file for encapsulation
    private class RingImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _center;
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
            _center = center;
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
                _lr.SetPosition(i, _center + new Vector3(x, y + verticalOffset, 0f));
            }
        }
    }

    private class CrossImpactBehavior : MonoBehaviour
    {
        private LineRenderer _lr;
        private Vector3 _center;
        private float _lifetime;
        private float _elapsed;
        private float _size;
        private float _verticalSpeed;
        private Color _startColor;
        private Color _endColor;

        public void Initialize(LineRenderer lr, Vector3 center, float lifetime, float size, float verticalSpeed, Color color)
        {
            _lr = lr;
            _center = center;
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

            Vector3 centerOffset = _center + new Vector3(0f, verticalOffset, 0f);
            
            _lr.positionCount = 5;
            _lr.SetPosition(0, centerOffset + Vector3.left * _size);
            _lr.SetPosition(1, centerOffset + Vector3.right * _size);
            _lr.SetPosition(2, centerOffset);
            _lr.SetPosition(3, centerOffset + Vector3.up * _size);
            _lr.SetPosition(4, centerOffset + Vector3.down * _size);
        }
    }
}

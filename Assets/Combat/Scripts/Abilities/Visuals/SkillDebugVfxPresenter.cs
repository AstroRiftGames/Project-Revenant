using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillDebugVfxPresenter : MonoBehaviour
{
    [SerializeField] private bool _enabled = true;
    [SerializeField] private Transform _runtimeRoot;
    [SerializeField] private float _defaultDuration = 0.6f;
    [SerializeField] private float _primaryPointSize = 0.42f;
    [SerializeField] private float _secondaryPointSize = 0.28f;
    [SerializeField] private float _lineWidth = 0.08f;
    [SerializeField] private float _ringWidth = 0.07f;
    [SerializeField] private SkillDebugVfxInstance _targetPointPrefab;
    [SerializeField] private SkillDebugVfxInstance _areaCirclePrefab;
    [SerializeField] private SkillDebugVfxInstance _linePrefab;
    [SerializeField] private SkillDebugVfxInstance _impactSecondaryPrefab;
    [SerializeField] private SkillDebugVfxInstance _bounceLinkPrefab;
    [SerializeField] private SkillDebugVfxInstance _summonPrefab;
    [SerializeField] private SkillDebugVfxInstance _healPrefab;
    [SerializeField] private SkillDebugVfxInstance _statusPrefab;
    [SerializeField] private SkillDebugVfxInstance _knockbackPrefab;
    [Header("Debug Colors")]
    [SerializeField] private Color _damageColor = new Color(1f, 0.35f, 0.2f, 0.95f);
    [SerializeField] private Color _healColor = new Color(0.3f, 1f, 0.45f, 0.95f);
    [SerializeField] private Color _buffColor = new Color(0.25f, 0.75f, 1f, 0.95f);
    [SerializeField] private Color _debuffColor = new Color(0.72f, 0.35f, 0.95f, 0.95f);
    [SerializeField] private Color _summonColor = new Color(0.25f, 1f, 1f, 0.95f);
    [SerializeField] private Color _areaColor = new Color(1f, 0.82f, 0.22f, 0.9f);
    [SerializeField] private Color _bounceColor = new Color(0.75f, 0.95f, 1f, 0.95f);
    [SerializeField] private Color _lineColor = new Color(1f, 0.55f, 0.45f, 0.92f);

    private void OnEnable()
    {
        SkillCaster.AnySkillImpactsResolvedForVisuals += HandleSkillImpactsResolved;
    }

    private void OnDisable()
    {
        SkillCaster.AnySkillImpactsResolvedForVisuals -= HandleSkillImpactsResolved;
    }

    private void HandleSkillImpactsResolved(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (!_enabled || skill == null || context == null)
            return;

        PresentPattern(skill, context, impacts);
        PresentModifierFeedback(skill, context, impacts);
        PresentImpactMarkers(skill, context, impacts);
        PresentEffectFeedback(skill, context, impacts);
    }

    private void PresentPattern(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        switch (skill.ImpactPattern)
        {
            case ImpactPattern.Area:
                SpawnAreaCircle(
                    ResolveImpactCenterWorldPosition(context),
                    context.HasImpactCenterUnit ? context.ImpactCenterUnit : null,
                    context.HasImpactCenterUnit,
                    Mathf.Max(0.4f, skill.RadiusInCells),
                    _areaColor,
                    _defaultDuration);
                break;

            case ImpactPattern.Line:
                SpawnLineVisual(skill, context, impacts, _lineColor, _defaultDuration);
                break;
        }
    }

    private void PresentModifierFeedback(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (skill.Modifiers == null || skill.Modifiers.Length == 0)
            return;

        for (int i = 0; i < skill.Modifiers.Length; i++)
        {
            SkillModifier modifier = skill.Modifiers[i];
            if (modifier == null)
                continue;

            if (modifier is SplashSkillModifier)
            {
                SkillImpact primaryImpact = FindPrimaryImpact(impacts);
                Vector3 center = ResolveImpactWorldPosition(primaryImpact, context);
                Unit centerUnit = primaryImpact != null && primaryImpact.HasTargetUnit ? primaryImpact.TargetUnit : null;
                SpawnAreaCircle(center, centerUnit, centerUnit != null, Mathf.Max(0.4f, skill.RadiusInCells), _areaColor, _defaultDuration);
                continue;
            }

            if (modifier is ExplosiveSkillModifier explosiveModifier)
            {
                int radius = explosiveModifier.ExplosionRadiusInCells;
                float worldRadius = Mathf.Max(0.4f, radius > 0 ? radius : skill.RadiusInCells);

                for (int impactIndex = 0; impacts != null && impactIndex < impacts.Count; impactIndex++)
                {
                    SkillImpact impact = impacts[impactIndex];
                    if (impact == null)
                        continue;

                    SpawnAreaCircle(
                        ResolveImpactWorldPosition(impact, context),
                        impact.HasTargetUnit ? impact.TargetUnit : null,
                        impact.HasTargetUnit,
                        worldRadius,
                        _areaColor,
                        _defaultDuration * 0.95f);
                }

                continue;
            }

            if (modifier is BounceSkillModifier)
            {
                PresentBounceLinks(impacts);
            }
        }
    }

    private void PresentImpactMarkers(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (impacts == null || impacts.Count == 0)
            return;

        Color primaryColor = ResolvePrimaryImpactColor(skill);
        Color secondaryColor = ResolveSecondaryImpactColor(skill);

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null)
                continue;

            Vector3 worldPosition = ResolveImpactWorldPosition(impact, context);
            Unit targetUnit = impact.HasTargetUnit ? impact.TargetUnit : null;

            if (impact.IsPrimaryImpact)
            {
                SkillDebugVfxInstance instance = SpawnInstance(_targetPointPrefab, "VFX_TargetPoint_Runtime");
                instance.ConfigureDiamond(worldPosition, targetUnit, targetUnit != null, _primaryPointSize, primaryColor, _defaultDuration);
                continue;
            }

            SkillDebugVfxInstance secondaryInstance = SpawnInstance(_impactSecondaryPrefab, "VFX_ImpactSecondary_Runtime");
            secondaryInstance.ConfigurePoint(worldPosition, targetUnit, targetUnit != null, _secondaryPointSize, secondaryColor, _defaultDuration);
        }
    }

    private void PresentEffectFeedback(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (skill.Effects == null || skill.Effects.Length == 0)
            return;

        bool hasSummonFeedback = false;

        for (int effectIndex = 0; effectIndex < skill.Effects.Length; effectIndex++)
        {
            SkillEffect effect = skill.Effects[effectIndex];
            if (effect == null)
                continue;

            if (effect is HealSkillEffect)
            {
                PresentHealFeedback(context, impacts);
                continue;
            }

            if (effect is ApplyStatusSkillEffect applyStatusSkillEffect)
            {
                PresentStatusFeedback(context, impacts, applyStatusSkillEffect);
                continue;
            }

            if (effect is SummonUnitSkillEffect summonUnitSkillEffect)
            {
                PresentSummonFeedback(context, summonUnitSkillEffect);
                hasSummonFeedback = true;
                continue;
            }

            if (effect is KnockbackSkillEffect knockbackSkillEffect)
            {
                PresentKnockbackFeedback(context, impacts, knockbackSkillEffect);
            }
        }

        if (!hasSummonFeedback)
            return;
    }

    private void PresentHealFeedback(SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (impacts == null)
            return;

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            SkillDebugVfxInstance instance = SpawnInstance(_healPrefab, "VFX_Heal_Runtime");
            instance.ConfigureCross(impact.TargetUnit.Position, impact.TargetUnit, true, 0.46f, 0.12f, _healColor, _defaultDuration);
        }
    }

    private void PresentStatusFeedback(
        SkillContext context,
        IReadOnlyList<SkillImpact> impacts,
        ApplyStatusSkillEffect applyStatusSkillEffect)
    {
        if (impacts == null || applyStatusSkillEffect == null)
            return;

        Color statusColor = ResolveStatusColor(applyStatusSkillEffect);
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            SkillDebugVfxInstance instance = SpawnInstance(_statusPrefab, "VFX_Status_Runtime");
            instance.ConfigureRing(impact.TargetUnit.Position, impact.TargetUnit, true, 0.34f, _ringWidth, statusColor, _defaultDuration);
        }
    }

    private void PresentSummonFeedback(SkillContext context, SummonUnitSkillEffect summonUnitSkillEffect)
    {
        Vector3 worldPosition = ResolveSummonAnchorWorldPosition(context, summonUnitSkillEffect);
        SkillDebugVfxInstance instance = SpawnInstance(_summonPrefab, "VFX_Summon_Runtime");
        instance.ConfigureRing(worldPosition, null, false, Mathf.Max(0.45f, summonUnitSkillEffect.SpawnRangeInCells), _ringWidth, _summonColor, _defaultDuration + 0.15f);
    }

    private void PresentKnockbackFeedback(
        SkillContext context,
        IReadOnlyList<SkillImpact> impacts,
        KnockbackSkillEffect knockbackSkillEffect)
    {
        if (impacts == null || context == null || knockbackSkillEffect == null || context.Caster == null)
            return;

        int knockbackCells = knockbackSkillEffect.KnockbackCells;
        if (knockbackCells <= 0)
            return;

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            Vector3 direction = impact.TargetUnit.Position - context.Caster.Position;
            direction.z = 0f;
            if (direction.sqrMagnitude < Mathf.Epsilon)
                continue;

            Vector3 end = impact.TargetUnit.Position + direction.normalized * knockbackCells;
            SkillDebugVfxInstance instance = SpawnInstance(_knockbackPrefab, "VFX_Knockback_Runtime");
            instance.ConfigureArrow(impact.TargetUnit.Position, end, _lineWidth, _damageColor, _defaultDuration);
        }
    }

    private void PresentBounceLinks(IReadOnlyList<SkillImpact> impacts)
    {
        if (impacts == null || impacts.Count < 2)
            return;

        var orderedImpacts = new List<SkillImpact>();
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            InsertImpactByChainIndex(orderedImpacts, impact);
        }

        if (orderedImpacts.Count < 2)
            return;

        for (int i = 1; i < orderedImpacts.Count; i++)
        {
            SkillImpact previousImpact = orderedImpacts[i - 1];
            SkillImpact currentImpact = orderedImpacts[i];
            if (previousImpact == null || currentImpact == null)
                continue;

            SkillDebugVfxInstance instance = SpawnInstance(_bounceLinkPrefab, "VFX_BounceLink_Runtime");
            instance.ConfigureLine(previousImpact.TargetUnit.Position, currentImpact.TargetUnit.Position, _lineWidth, _bounceColor, _defaultDuration);
        }
    }

    private void SpawnLineVisual(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts, Color color, float duration)
    {
        if (context == null || context.Caster == null)
            return;

        Vector3 start = context.Caster.Position;
        Vector3 end = ResolveLineEndWorldPosition(skill, context, impacts);

        SkillDebugVfxInstance instance = SpawnInstance(_linePrefab, "VFX_Line_Runtime");
        instance.ConfigureLine(start, end, _lineWidth, color, duration);
    }

    private void SpawnAreaCircle(
        Vector3 worldPosition,
        Unit targetUnit,
        bool followTarget,
        float radius,
        Color color,
        float duration)
    {
        SkillDebugVfxInstance instance = SpawnInstance(_areaCirclePrefab, "VFX_AreaCircle_Runtime");
        instance.ConfigureRing(worldPosition, targetUnit, followTarget, radius, _ringWidth, color, duration);
    }

    private SkillDebugVfxInstance SpawnInstance(SkillDebugVfxInstance prefab, string fallbackName)
    {
        Transform parent = _runtimeRoot != null ? _runtimeRoot : transform;
        SkillDebugVfxInstance instance = prefab != null
            ? Instantiate(prefab, parent)
            : null;

        if (instance == null)
        {
            GameObject gameObject = new GameObject(fallbackName);
            gameObject.transform.SetParent(parent, false);
            instance = gameObject.AddComponent<SkillDebugVfxInstance>();
        }

        instance.gameObject.name = fallbackName;
        return instance;
    }

    private static void InsertImpactByChainIndex(List<SkillImpact> orderedImpacts, SkillImpact impact)
    {
        if (orderedImpacts == null || impact == null)
            return;

        int insertIndex = orderedImpacts.Count;
        for (int i = 0; i < orderedImpacts.Count; i++)
        {
            SkillImpact candidate = orderedImpacts[i];
            if (candidate == null)
                continue;

            if (impact.ChainIndex < candidate.ChainIndex)
            {
                insertIndex = i;
                break;
            }
        }

        orderedImpacts.Insert(insertIndex, impact);
    }

    private static SkillImpact FindPrimaryImpact(IReadOnlyList<SkillImpact> impacts)
    {
        if (impacts == null)
            return null;

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact != null && impact.IsPrimaryImpact)
                return impact;
        }

        return impacts.Count > 0 ? impacts[0] : null;
    }

    private Color ResolvePrimaryImpactColor(SkillData skill)
    {
        if (skill == null || skill.Effects == null)
            return _damageColor;

        for (int i = 0; i < skill.Effects.Length; i++)
        {
            SkillEffect effect = skill.Effects[i];
            if (effect is HealSkillEffect)
                return _healColor;

            if (effect is SummonUnitSkillEffect)
                return _summonColor;
        }

        return _damageColor;
    }

    private Color ResolveSecondaryImpactColor(SkillData skill)
    {
        if (HasModifier<BounceSkillModifier>(skill))
            return _bounceColor;

        if (HasModifier<ExplosiveSkillModifier>(skill) || HasModifier<SplashSkillModifier>(skill))
            return _areaColor;

        return ResolvePrimaryImpactColor(skill);
    }

    private Color ResolveStatusColor(ApplyStatusSkillEffect applyStatusSkillEffect)
    {
        if (applyStatusSkillEffect == null || applyStatusSkillEffect.StatusDefinitions == null)
            return _debuffColor;

        for (int i = 0; i < applyStatusSkillEffect.StatusDefinitions.Length; i++)
        {
            StatusEffectDefinition definition = applyStatusSkillEffect.StatusDefinitions[i];
            if (definition == null)
                continue;

            switch (definition.EffectType)
            {
                case StatusEffectType.Heal:
                case StatusEffectType.HealOverTime:
                    return _healColor;

                case StatusEffectType.StatModifierBuff:
                    return _buffColor;

                case StatusEffectType.Taunt:
                case StatusEffectType.Stun:
                case StatusEffectType.DamageOverTime:
                case StatusEffectType.StatModifierDebuff:
                default:
                    return _debuffColor;
            }
        }

        return _debuffColor;
    }

    private static bool HasModifier<TModifier>(SkillData skill) where TModifier : SkillModifier
    {
        if (skill == null || skill.Modifiers == null)
            return false;

        for (int i = 0; i < skill.Modifiers.Length; i++)
        {
            if (skill.Modifiers[i] is TModifier)
                return true;
        }

        return false;
    }

    private static Vector3 ResolveImpactCenterWorldPosition(SkillContext context)
    {
        if (context == null)
            return Vector3.zero;

        if (context.HasImpactCenterUnit)
            return context.ImpactCenterUnit.Position;

        return context.ImpactCenterWorld;
    }

    private static Vector3 ResolveImpactWorldPosition(SkillImpact impact, SkillContext context)
    {
        if (impact != null)
        {
            if (impact.HasTargetUnit)
                return impact.TargetUnit.Position;

            if (impact.Kind == SkillImpactKind.AreaPoint)
                return impact.WorldPosition;

            if (impact.HasCell && context != null && context.RoomGrid != null)
                return context.RoomGrid.CellToWorld(new Vector3Int(impact.Cell.x, impact.Cell.y, 0));
        }

        return ResolveImpactCenterWorldPosition(context);
    }

    private static Vector3 ResolveLineEndWorldPosition(SkillData skill, SkillContext context, IReadOnlyList<SkillImpact> impacts)
    {
        if (context == null || context.Caster == null)
            return Vector3.zero;

        Vector3 start = context.Caster.Position;

        if (impacts != null && impacts.Count > 0)
        {
            SkillImpact lastImpact = impacts[0];
            for (int i = 1; i < impacts.Count; i++)
            {
                SkillImpact candidate = impacts[i];
                if (candidate == null || lastImpact == null)
                    continue;

                if (candidate.ChainIndex >= lastImpact.ChainIndex)
                    lastImpact = candidate;
            }

            return ResolveImpactWorldPosition(lastImpact, context);
        }

        Vector3 fallbackTarget = context.HasPrimaryTarget
            ? context.PrimaryTarget.Position
            : context.ImpactCenterWorld;
        Vector3 direction = fallbackTarget - start;
        direction.z = 0f;

        if (direction.sqrMagnitude < Mathf.Epsilon)
            direction = Vector3.right;
        else
            direction.Normalize();

        return start + direction * Mathf.Max(1f, skill != null ? skill.LineLengthInCells : 1f);
    }

    private static Vector3 ResolveSummonAnchorWorldPosition(SkillContext context, SummonUnitSkillEffect summonUnitSkillEffect)
    {
        if (context == null || summonUnitSkillEffect == null)
            return Vector3.zero;

        switch (summonUnitSkillEffect.AnchorMode)
        {
            case SummonAnchorMode.AroundCaster:
                return context.Caster != null ? context.Caster.Position : context.ImpactCenterWorld;

            case SummonAnchorMode.AroundPrimaryTarget:
                return context.PrimaryTarget != null ? context.PrimaryTarget.Position : context.ImpactCenterWorld;

            case SummonAnchorMode.AroundImpactCenter:
                return ResolveImpactCenterWorldPosition(context);

            case SummonAnchorMode.AtTargetCell:
                if (context.HasTargetCell && context.RoomGrid != null)
                    return context.RoomGrid.CellToWorld(new Vector3Int(context.TargetCell.x, context.TargetCell.y, 0));
                return context.ImpactCenterWorld;

            default:
                return context.Caster != null ? context.Caster.Position : context.ImpactCenterWorld;
        }
    }
}

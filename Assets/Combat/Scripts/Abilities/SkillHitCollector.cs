using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillHitCollector
{
    // PrimaryTarget is the tactical unit chosen earlier by targeting.
    // ImpactTarget is the unit finally affected after shape resolution.
    // ImpactTargetRequirement applies only when validating impacted units.
    public static bool CanSkillHitUnit(SkillContext skillContext, Unit target, bool allowCasterForSelfCenteredSkill = false)
    {
        Unit caster = skillContext != null ? skillContext.Caster : null;
        SkillData skill = skillContext != null ? skillContext.Skill : null;
        if (caster == null || target == null || skill == null)
            return false;

        if (!skill.TryValidateDeclarativeContract(out _))
            return false;

        ImpactTargetRequirement impactTargetRequirement = skill.ImpactTargetRequirement;
        bool requireSelf = impactTargetRequirement == ImpactTargetRequirement.Self;
        bool allowSelf = requireSelf ||
                         impactTargetRequirement == ImpactTargetRequirement.Any ||
                         allowCasterForSelfCenteredSkill;
        TargetingPolicy policy = new(
            ResolveImpactTargetRelation(impactTargetRequirement),
            requiresTarget: false,
            allowSelf: allowSelf,
            requireSelf: requireSelf);
        return TargetingStrategy.IsTargetSelectable(caster, target, policy);
    }

    private readonly struct SkillTargetRequest
    {
        public SkillTargetRequest(SkillContext skillContext)
        {
            Context = skillContext;
        }

        public SkillContext Context { get; }
        public Unit Caster => Context != null ? Context.Caster : null;
        public SkillData Skill => Context != null ? Context.Skill : null;
        public Unit PrimaryTarget => Context != null ? Context.PrimaryTarget : null;
        public Unit ImpactCenterUnit => Context != null ? Context.ImpactCenterUnit : null;
        public bool HasTargetCell => Context != null && Context.HasTargetCell;
        public Vector2Int TargetCell => Context != null ? Context.TargetCell : default;
        public Vector3 ImpactCenterWorld => Context != null ? Context.ImpactCenterWorld : Vector3.zero;
        public RoomGrid RoomGrid => Context != null ? Context.RoomGrid : null;
        public ImpactPattern ImpactPattern => Skill != null ? Skill.ImpactPattern : ImpactPattern.Direct;
    }

    public static bool TryCollectImpacts(SkillContext skillContext, List<SkillImpact> results, Action<string> debugLog = null)
    {
        if (results == null)
            return false;

        results.Clear();

        if (skillContext == null || skillContext.Caster == null || skillContext.Skill == null)
            return false;

        SkillTargetRequest request = new(skillContext);
        bool collectedTargets = TryCollectBaseImpacts(request, results, debugLog);

        if (collectedTargets)
        {
            return true;
        }

        if (CanResolveWithoutImpactTargets(request.Skill))
        {
            TryCreateFallbackImpact(request, results);
            debugLog?.Invoke(
                $"[SkillHitCollector] {FormatUnit(request.Caster)} resolved '{request.Skill.DisplayName}' without unit impacts " +
                $"because its payload does not require impacted units. Compatibility impacts: {FormatImpacts(results)}.");
            return true;
        }

        return false;
    }

    private static bool TryCollectBaseImpacts(SkillTargetRequest request, List<SkillImpact> results, Action<string> debugLog)
    {
        ImpactPattern impactPattern = request.Skill != null
            ? request.Skill.ImpactPattern
            : ImpactPattern.Direct;

        return impactPattern switch
        {
            ImpactPattern.Direct => TryCollectSingleTarget(request, results, debugLog),
            ImpactPattern.Area => TryCollectAreaTargets(request, results, debugLog),
            ImpactPattern.Line => TryCollectLineTargets(request, results, debugLog),
            ImpactPattern.MultiTarget => TryCollectMultiTarget(request, results, debugLog),
            _ => false
        };
    }

    private static bool TryGetRequestData(SkillTargetRequest request, out Unit caster, out SkillData skill)
    {
        caster = request.Caster;
        skill = request.Skill;
        return caster != null && skill != null;
    }

    private static bool TryCollectSingleTarget(SkillTargetRequest request, List<SkillImpact> results, Action<string> debugLog)
    {
        Unit primaryTarget = request.PrimaryTarget;
        if (primaryTarget == null || !CanUseUnitAsImpactTarget(request, primaryTarget))
            return false;

        TryAddUnitImpact(results, primaryTarget, 0, true);
        debugLog?.Invoke(
            $"[SkillHitCollector] {FormatUnit(request.Caster)} pattern '{request.ImpactPattern}' resolved primary target " +
            $"{FormatUnit(primaryTarget)}. Impacted: {FormatImpacts(results)}.");
        return true;
    }

    private static bool TryCollectAreaTargets(SkillTargetRequest request, List<SkillImpact> results, Action<string> debugLog)
    {
        return TryCollectImpactsInRadius(request, results, debugLog, includePrimaryTargetFirst: false, "area");
    }

    private static bool TryCollectLineTargets(SkillTargetRequest request, List<SkillImpact> results, Action<string> debugLog)
    {
        if (!TryBuildLineResolution(request, out LineResolution resolution))
            return false;

        if (resolution.Projections.Count > 0)
            TryAddUnitImpact(results, resolution.Projections[0].Target, 0, true);

        debugLog?.Invoke(
            $"[SkillHitCollector] {FormatUnit(resolution.Caster)} pattern '{resolution.ImpactPattern}' resolved line. " +
            $"PrimaryTarget: {FormatUnit(resolution.PrimaryTarget)}. ImpactCenter: {FormatWorldPosition(resolution.LineOrigin)}. " +
            $"Direction: {FormatWorldDirection(resolution.LineDirection)}. Length: {resolution.LineLengthInCells} cells ({resolution.LineLengthWorld:F2} world). " +
            $"Tolerance: {resolution.LineTolerance:F2}. First impact: {FormatUnit(results.Count > 0 ? results[0].TargetUnit : null)}. " +
            $"Ordered projections: {FormatProjectedUnits(resolution.Projections)}. " +
            $"Skipped invalid/allied/dead: {resolution.SkippedInvalid}. Skipped behind caster: {resolution.SkippedBehindCaster}. " +
            $"Skipped past length: {resolution.SkippedPastLength}. Skipped off line: {resolution.SkippedOffLine}.");

        return results.Count > 0;
    }

    private static bool TryCollectMultiTarget(SkillTargetRequest request, List<SkillImpact> results, Action<string> debugLog)
    {
        if (!TryResolveImpactCenter(
                request,
                out Unit primaryTarget,
                out Unit impactCenterUnit,
                out Vector3 impactCenterWorld,
                out bool impactCenterUsesTargetCell))
            return false;

        bool seedResultsWithPrimaryTarget =
            primaryTarget != null &&
            CanUseUnitAsImpactTarget(request, primaryTarget) &&
            IsWithinImpactRadius(request, impactCenterUnit, impactCenterWorld, primaryTarget);
        if (seedResultsWithPrimaryTarget)
            TryAddUnitImpact(results, primaryTarget, 0, true);

        if (!TryGetRequestData(request, out Unit caster, out SkillData skill))
            return false;

        IReadOnlyList<Unit> roomUnits = GetRoomUnits(caster);
        int maxTargets = skill != null ? skill.MaxTargets : 1;
        int skippedDuplicatePrimary = 0;
        int skippedInvalid = 0;
        int skippedOutOfRadius = 0;
        int skippedOverLimit = 0;

        if (roomUnits != null && maxTargets > 1)
        {
            var candidates = new List<DistanceCandidate>(roomUnits.Count);

            for (int i = 0; i < roomUnits.Count; i++)
            {
                Unit candidate = roomUnits[i];
                if (seedResultsWithPrimaryTarget && ReferenceEquals(candidate, primaryTarget))
                {
                    skippedDuplicatePrimary++;
                    continue;
                }

                if (!CanUseUnitAsImpactTarget(request, candidate))
                {
                    skippedInvalid++;
                    continue;
                }

                if (!IsWithinImpactRadius(request, impactCenterUnit, impactCenterWorld, candidate))
                {
                    skippedOutOfRadius++;
                    continue;
                }

                candidates.Add(new DistanceCandidate(candidate, ResolveDistanceFromImpactCenter(request, impactCenterUnit, candidate)));
            }

            candidates.Sort(static (left, right) =>
            {
                int distanceComparison = left.Distance.CompareTo(right.Distance);
                if (distanceComparison != 0)
                    return distanceComparison;

                return left.Target.GetEntityId().CompareTo(right.Target.GetEntityId());
            });

            int remainingSlots = Mathf.Max(0, maxTargets - results.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i < remainingSlots)
                {
                    bool isPrimaryImpact = results.Count == 0;
                    TryAddUnitImpact(results, candidates[i].Target, results.Count, isPrimaryImpact);
                    continue;
                }

                skippedOverLimit++;
            }
        }

        debugLog?.Invoke(
            $"[SkillHitCollector] {FormatUnit(caster)} pattern '{request.ImpactPattern}' resolved multi target. " +
            $"PrimaryTarget: {FormatUnit(primaryTarget)}. ImpactCenter: {FormatWorldPosition(impactCenterWorld)}. " +
            $"Radius: {skill.RadiusInCells}. Max targets: {maxTargets}. " +
            $"Impacted ({results.Count}): {FormatImpacts(results)}. " +
            $"Skipped duplicate primary: {skippedDuplicatePrimary}. " +
            $"Skipped invalid/allied/dead: {skippedInvalid}. Skipped out of radius: {skippedOutOfRadius}. " +
            $"Skipped over limit: {skippedOverLimit}.");

        return results.Count > 0;
    }

    private static bool TryCollectImpactsInRadius(
        SkillTargetRequest request,
        List<SkillImpact> results,
        Action<string> debugLog,
        bool includePrimaryTargetFirst,
        string shapeName)
    {
        if (!TryResolveImpactCenter(
                request,
                out Unit primaryTarget,
                out Unit impactCenterUnit,
                out Vector3 impactCenterWorld,
                out bool impactCenterUsesTargetCell))
            return false;

        bool seedResultsWithPrimaryTarget =
            includePrimaryTargetFirst &&
            primaryTarget != null &&
            CanUseUnitAsImpactTarget(request, primaryTarget) &&
            IsWithinImpactRadius(request, impactCenterUnit, impactCenterWorld, primaryTarget);
        if (seedResultsWithPrimaryTarget)
            TryAddUnitImpact(results, primaryTarget, 0, true);

        int skippedDuplicatePrimary = 0;
        int skippedInvalid = 0;
        int skippedOutOfRadius = 0;

        if (!TryGetRequestData(request, out Unit caster, out SkillData skill))
            return false;

        IReadOnlyList<Unit> roomUnits = GetRoomUnits(caster);
        if (roomUnits == null)
        {
            if (!includePrimaryTargetFirst &&
                primaryTarget != null &&
                CanUseUnitAsImpactTarget(request, primaryTarget) &&
                IsWithinImpactRadius(request, impactCenterUnit, impactCenterWorld, primaryTarget))
            {
                TryAddUnitImpact(results, primaryTarget, 0, true);
            }

            debugLog?.Invoke(
                $"[SkillHitCollector] {FormatUnit(caster)} pattern '{request.ImpactPattern}' resolved {shapeName} with only " +
                $"primary target {FormatUnit(primaryTarget)} because no room unit list was available.");
            return results.Count > 0;
        }

        bool hasPrimaryImpact = seedResultsWithPrimaryTarget;
        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (seedResultsWithPrimaryTarget && ReferenceEquals(candidate, primaryTarget))
            {
                skippedDuplicatePrimary++;
                continue;
            }

            if (!CanUseUnitAsImpactTarget(request, candidate))
            {
                skippedInvalid++;
                continue;
            }

            if (!IsWithinImpactRadius(request, impactCenterUnit, impactCenterWorld, candidate))
            {
                skippedOutOfRadius++;
                continue;
            }

            bool isPrimaryImpact = !hasPrimaryImpact && ReferenceEquals(candidate, primaryTarget);
            if (TryAddUnitImpact(results, candidate, results.Count, isPrimaryImpact) && isPrimaryImpact)
                hasPrimaryImpact = true;
        }

        debugLog?.Invoke(
            $"[SkillHitCollector] {FormatUnit(caster)} pattern '{request.ImpactPattern}' resolved {shapeName}. " +
            $"PrimaryTarget: {FormatUnit(primaryTarget)}. ImpactCenter: {FormatWorldPosition(impactCenterWorld)}. " +
            $"Radius: {skill.RadiusInCells}. " +
            $"Impacted ({results.Count}): {FormatImpacts(results)}. " +
            $"Skipped duplicate primary: {skippedDuplicatePrimary}. " +
            $"Skipped invalid/allied/dead: {skippedInvalid}. Skipped out of radius: {skippedOutOfRadius}.");

        return results.Count > 0;
    }

    private static bool TryResolveImpactCenter(
        SkillTargetRequest request,
        out Unit primaryTarget,
        out Unit impactCenterUnit,
        out Vector3 impactCenterWorld,
        out bool impactCenterUsesTargetCell)
    {
        primaryTarget = request.PrimaryTarget;
        impactCenterUnit = request.ImpactCenterUnit;
        impactCenterWorld = request.ImpactCenterWorld;
        impactCenterUsesTargetCell = request.Skill != null && request.Skill.ImpactCenterMode == ImpactCenterMode.TargetCell;

        if (impactCenterUsesTargetCell)
            return request.HasTargetCell;

        return impactCenterUnit != null;
    }

    private static bool CanUseUnitAsImpactTarget(SkillTargetRequest request, Unit candidate)
    {
        return TryGetRequestData(request, out _, out _) &&
               CanSkillHitUnit(request.Context, candidate);
    }

    private static bool IsWithinImpactRadius(SkillTargetRequest request, Unit centerUnit, Vector3 centerWorld, Unit candidate)
    {
        if (candidate == null)
            return false;

        SkillData skill = request.Skill;
        int radiusInCells = skill != null ? skill.RadiusInCells : 0;
        if (radiusInCells <= 0)
            return false;

        RoomGrid grid = request.RoomGrid;
        if (grid == null)
        {
            Vector3 resolvedCenterWorld = centerUnit != null ? centerUnit.Position : centerWorld;
            float distance = Vector3.Distance(resolvedCenterWorld, candidate.Position);
            return distance <= radiusInCells;
        }

        Vector3Int centerCell;
        if (request.HasTargetCell)
        {
            centerCell = new Vector3Int(request.TargetCell.x, request.TargetCell.y, 0);
        }
        else if (centerUnit != null)
        {
            centerCell = ResolveUnitCell(grid, centerUnit);
        }
        else
        {
            return false;
        }

        Vector3Int candidateCell = ResolveUnitCell(grid, candidate);
        return GridNavigationUtility.IsWithinCellRange(centerCell, candidateCell, radiusInCells);
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        return GridUnitCellUtility.ResolveUnitCell(grid, unit);
    }

    private static IReadOnlyList<Unit> GetRoomUnits(Unit caster)
    {
        return caster != null
            ? caster.GetRoomUnits()
            : Array.Empty<Unit>();
    }

    private static bool TryResolveLineProjection(
        Vector3 lineOrigin,
        Vector3 lineDirection,
        Vector3 point,
        out float projection,
        out float distanceToLine)
    {
        Vector3 offset = point - lineOrigin;
        offset.z = 0f;

        float lineDirectionMagnitude = lineDirection.magnitude;
        if (lineDirectionMagnitude <= Mathf.Epsilon)
        {
            projection = 0f;
            distanceToLine = float.MaxValue;
            return false;
        }

        projection = Vector3.Dot(offset, lineDirection);
        Vector3 closestPoint = lineOrigin + lineDirection * projection;
        distanceToLine = Vector3.Distance(
            new Vector3(point.x, point.y, 0f),
            new Vector3(closestPoint.x, closestPoint.y, 0f));
        return true;
    }

    private static float ResolveLineLengthWorld(SkillTargetRequest request, int lineLengthInCells)
    {
        if (request.Caster == null || request.Caster.RoomContext == null || request.Caster.RoomContext.RoomGrid == null)
            return Mathf.Max(0f, lineLengthInCells);

        Vector2 cellWorldSize = request.Caster.RoomContext.RoomGrid.CellWorldSize;
        float cellStep = Mathf.Max(Mathf.Abs(cellWorldSize.x), Mathf.Abs(cellWorldSize.y));
        return Mathf.Max(0f, lineLengthInCells) * Mathf.Max(0.01f, cellStep);
    }

    private static float ResolveLineTolerance(SkillTargetRequest request)
    {
        if (request.Caster == null || request.Caster.RoomContext == null || request.Caster.RoomContext.RoomGrid == null)
            return 0.5f;

        Vector2 cellWorldSize = request.Caster.RoomContext.RoomGrid.CellWorldSize;
        float cellStep = Mathf.Max(Mathf.Abs(cellWorldSize.x), Mathf.Abs(cellWorldSize.y));
        return Mathf.Max(0.1f, cellStep * 0.35f);
    }

    private static float ResolveDistanceFromImpactCenter(SkillTargetRequest request, Unit impactCenterUnit, Unit candidate)
    {
        if (candidate == null)
            return float.MaxValue;

        RoomGrid grid = request.RoomGrid;
        if (grid == null)
        {
            Vector3 impactCenterWorld = impactCenterUnit != null ? impactCenterUnit.Position : request.ImpactCenterWorld;
            return Vector3.Distance(impactCenterWorld, candidate.Position);
        }

        Vector3Int centerCell;
        if (request.HasTargetCell)
        {
            centerCell = new Vector3Int(request.TargetCell.x, request.TargetCell.y, 0);
        }
        else if (impactCenterUnit != null)
        {
            centerCell = ResolveUnitCell(grid, impactCenterUnit);
        }
        else
        {
            return float.MaxValue;
        }

        Vector3Int candidateCell = ResolveUnitCell(grid, candidate);
        return GridNavigationUtility.GetCellDistance(centerCell, candidateCell);
    }

    private static bool TryBuildLineResolution(SkillTargetRequest request, out LineResolution resolution)
    {
        resolution = default;

        Unit caster = request.Caster;
        Unit primaryTarget = request.PrimaryTarget;
        SkillData skill = request.Skill;
        if (caster == null || primaryTarget == null || skill == null || !CanUseUnitAsImpactTarget(request, primaryTarget))
            return false;

        if (!TryResolveImpactCenter(
                request,
                out _,
                out Unit impactCenterUnit,
                out Vector3 impactCenterWorld,
                out _))
        {
            return false;
        }

        int lineLengthInCells = skill.LineLengthInCells;
        if (lineLengthInCells <= 0)
            return false;

        IReadOnlyList<Unit> roomUnits = GetRoomUnits(caster);
        if (roomUnits == null)
            return false;

        Vector3 lineOrigin = impactCenterUnit != null ? impactCenterUnit.Position : impactCenterWorld;
        Vector3 lineDirection = primaryTarget.Position - lineOrigin;
        lineDirection.z = 0f;

        if (lineDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        lineDirection.Normalize();

        float lineLengthWorld = ResolveLineLengthWorld(request, lineLengthInCells);
        float lineTolerance = ResolveLineTolerance(request);
        int skippedInvalid = 0;
        int skippedBehindCaster = 0;
        int skippedPastLength = 0;
        int skippedOffLine = 0;

        var projections = new List<TargetProjection>(roomUnits.Count);

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!CanUseUnitAsImpactTarget(request, candidate))
            {
                skippedInvalid++;
                continue;
            }

            if (!TryResolveLineProjection(lineOrigin, lineDirection, candidate.Position, out float projection, out float distanceToLine))
            {
                skippedOffLine++;
                continue;
            }

            if (projection < 0f)
            {
                skippedBehindCaster++;
                continue;
            }

            if (projection > lineLengthWorld)
            {
                skippedPastLength++;
                continue;
            }

            if (distanceToLine > lineTolerance)
            {
                skippedOffLine++;
                continue;
            }

            projections.Add(new TargetProjection(candidate, projection));
        }

        projections.Sort(static (left, right) => left.Projection.CompareTo(right.Projection));

        resolution = new LineResolution(
            caster,
            primaryTarget,
            skill,
            lineOrigin,
            lineDirection,
            lineLengthInCells,
            lineLengthWorld,
            lineTolerance,
            skippedInvalid,
            skippedBehindCaster,
            skippedPastLength,
            skippedOffLine,
            projections);
        return true;
    }

    private static bool TryAddUnitImpact(List<SkillImpact> results, Unit candidate, int chainIndex, bool isPrimaryImpact)
    {
        SkillImpact impact = SkillImpact.CreateUnit(candidate, chainIndex, isPrimaryImpact);
        return TryAddUniqueImpact(results, impact);
    }

    private static bool TryAddUniqueImpact(List<SkillImpact> results, SkillImpact candidate)
    {
        if (results == null || candidate == null)
            return false;

        for (int i = 0; i < results.Count; i++)
        {
            SkillImpact existing = results[i];
            if (existing == null)
                continue;

            if (candidate.HasTargetUnit && existing.HasTargetUnit && ReferenceEquals(existing.TargetUnit, candidate.TargetUnit))
                return false;

            if (candidate.HasCell &&
                existing.HasCell &&
                existing.Kind == candidate.Kind &&
                existing.Cell == candidate.Cell)
            {
                return false;
            }

            if (!candidate.HasTargetUnit &&
                !candidate.HasCell &&
                !existing.HasTargetUnit &&
                !existing.HasCell &&
                existing.Kind == candidate.Kind &&
                existing.WorldPosition == candidate.WorldPosition)
            {
                return false;
            }
        }

        results.Add(candidate);
        return true;
    }

    private static void TryCreateFallbackImpact(SkillTargetRequest request, List<SkillImpact> results)
    {
        if (results == null)
            return;

        if (request.HasTargetCell)
        {
            TryAddUniqueImpact(results, SkillImpact.CreateCell(request.TargetCell, 0, true));
            return;
        }

        Vector3 worldPosition = request.ImpactCenterUnit != null
            ? request.ImpactCenterUnit.Position
            : request.ImpactCenterWorld;
        TryAddUniqueImpact(results, SkillImpact.CreateAreaPoint(worldPosition, 0, true));
    }



    private static TargetRelation ResolveImpactTargetRelation(ImpactTargetRequirement targetRequirement)
    {
        return targetRequirement switch
        {
            ImpactTargetRequirement.Hostile => TargetRelation.Hostile,
            ImpactTargetRequirement.Ally => TargetRelation.Ally,
            _ => TargetRelation.Any
        };
    }

    public static void ApplySplashModifierToImpacts(SkillContext skillContext, SkillData skill, List<SkillImpact> impacts)
    {
        if (skillContext == null || skill == null || impacts == null || impacts.Count == 0)
            return;

        SkillImpact primaryImpact = null;
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            if (impact.IsPrimaryImpact)
            {
                primaryImpact = impact;
                break;
            }

            if (primaryImpact == null)
                primaryImpact = impact;
        }

        if (primaryImpact == null)
            return;

        SkillTargetRequest request = new(skillContext);
        if (!TryResolveImpactCenter(
                request,
                out _,
                out Unit impactCenterUnit,
                out Vector3 impactCenterWorld,
                out _))
        {
            return;
        }

        if (!TryGetRequestData(request, out Unit caster, out _))
            return;

        IReadOnlyList<Unit> roomUnits = GetRoomUnits(caster);
        if (roomUnits == null)
            return;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!CanUseUnitAsImpactTarget(request, candidate))
                continue;

            if (!IsWithinImpactRadius(request, impactCenterUnit, impactCenterWorld, candidate))
                continue;

            bool isPrimaryImpact = ReferenceEquals(candidate, primaryImpact.TargetUnit);
            int chainIndex = isPrimaryImpact ? primaryImpact.ChainIndex : impacts.Count;
            TryAddUnitImpact(impacts, candidate, chainIndex, isPrimaryImpact);
        }
    }

    public static void ApplyPiercingModifierToImpacts(SkillContext skillContext, SkillData skill, List<SkillImpact> impacts)
    {
        if (skillContext == null || skill == null || impacts == null)
            return;

        SkillTargetRequest request = new(skillContext);
        if (!TryBuildLineResolution(request, out LineResolution resolution))
            return;

        SkillImpact primaryImpact = null;
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            if (impact.IsPrimaryImpact)
            {
                primaryImpact = impact;
                break;
            }

            if (primaryImpact == null)
                primaryImpact = impact;
        }

        if (primaryImpact != null)
        {
            primaryImpact.ChainIndex = 0;
            primaryImpact.IsPrimaryImpact = true;
        }

        for (int i = 0; i < resolution.Projections.Count; i++)
        {
            Unit target = resolution.Projections[i].Target;
            bool isPrimaryImpact = ReferenceEquals(primaryImpact != null ? primaryImpact.TargetUnit : null, target) ||
                                   (primaryImpact == null && i == 0);
            if (isPrimaryImpact && primaryImpact != null)
                continue;

            TryAddUnitImpact(impacts, target, i, isPrimaryImpact);
        }
    }

    public static bool CanResolveWithoutImpactTargets(SkillData skill)
    {
        if (skill == null)
            return false;

        var effects = skill.CompositionEffects;
        if (effects == null || effects.Length == 0)
            return false;

        bool foundEffect = false;
        for (int i = 0; i < effects.Length; i++)
        {
            foundEffect = true;
            if (effects[i].EffectKind != SkillEffectKind.Summon)
                return false;
        }

        return foundEffect;
    }

    private static string FormatUnits(List<Unit> units)
    {
        if (units == null || units.Count == 0)
            return "[None]";

        string[] labels = new string[units.Count];
        for (int i = 0; i < units.Count; i++)
            labels[i] = FormatUnit(units[i]);

        return string.Join(", ", labels);
    }

    private static string FormatImpacts(List<SkillImpact> impacts)
    {
        if (impacts == null || impacts.Count == 0)
            return "[None]";

        string[] labels = new string[impacts.Count];
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null)
            {
                labels[i] = "[NullImpact]";
                continue;
            }

            string primaryLabel = impact.IsPrimaryImpact ? "Primary" : "Secondary";
            if (impact.HasTargetUnit)
            {
                labels[i] = $"{FormatUnit(impact.TargetUnit)}|{primaryLabel}|Chain:{impact.ChainIndex}";
                continue;
            }

            if (impact.HasCell)
            {
                labels[i] = $"[Cell:{impact.Cell.x},{impact.Cell.y}|{primaryLabel}|Chain:{impact.ChainIndex}]";
                continue;
            }

            labels[i] = $"[Point:{FormatWorldPosition(impact.WorldPosition)}|{primaryLabel}|Chain:{impact.ChainIndex}]";
        }

        return string.Join(", ", labels);
    }

    private static string FormatUnit(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetEntityId()}|{unitId}]";
    }

    private static string FormatWorldPosition(Vector3 position)
    {
        return $"({position.x:F2}, {position.y:F2}, {position.z:F2})";
    }

    private static string FormatWorldDirection(Vector3 direction)
    {
        return $"({direction.x:F2}, {direction.y:F2}, {direction.z:F2})";
    }

    private static string FormatProjectedUnits(List<TargetProjection> projections)
    {
        if (projections == null || projections.Count == 0)
            return "[None]";

        string[] labels = new string[projections.Count];
        for (int i = 0; i < projections.Count; i++)
            labels[i] = $"{FormatUnit(projections[i].Target)}@{projections[i].Projection:F2}";

        return string.Join(", ", labels);
    }

    private readonly struct TargetProjection
    {
        public TargetProjection(Unit target, float projection)
        {
            Target = target;
            Projection = projection;
        }

        public Unit Target { get; }
        public float Projection { get; }
    }

    private readonly struct LineResolution
    {
        public LineResolution(
            Unit caster,
            Unit primaryTarget,
            SkillData skill,
            Vector3 lineOrigin,
            Vector3 lineDirection,
            int lineLengthInCells,
            float lineLengthWorld,
            float lineTolerance,
            int skippedInvalid,
            int skippedBehindCaster,
            int skippedPastLength,
            int skippedOffLine,
            List<TargetProjection> projections)
        {
            Caster = caster;
            PrimaryTarget = primaryTarget;
            Skill = skill;
            LineOrigin = lineOrigin;
            LineDirection = lineDirection;
            LineLengthInCells = lineLengthInCells;
            LineLengthWorld = lineLengthWorld;
            LineTolerance = lineTolerance;
            SkippedInvalid = skippedInvalid;
            SkippedBehindCaster = skippedBehindCaster;
            SkippedPastLength = skippedPastLength;
            SkippedOffLine = skippedOffLine;
            Projections = projections;
        }

        public Unit Caster { get; }
        public Unit PrimaryTarget { get; }
        public SkillData Skill { get; }
        public ImpactPattern ImpactPattern => Skill != null ? Skill.ImpactPattern : ImpactPattern.Direct;
        public Vector3 LineOrigin { get; }
        public Vector3 LineDirection { get; }
        public int LineLengthInCells { get; }
        public float LineLengthWorld { get; }
        public float LineTolerance { get; }
        public int SkippedInvalid { get; }
        public int SkippedBehindCaster { get; }
        public int SkippedPastLength { get; }
        public int SkippedOffLine { get; }
        public List<TargetProjection> Projections { get; }
    }

    private readonly struct DistanceCandidate
    {
        public DistanceCandidate(Unit target, float distance)
        {
            Target = target;
            Distance = distance;
        }

        public Unit Target { get; }
        public float Distance { get; }
    }
}

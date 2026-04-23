using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillTargetCollector
{
    public static bool TryCollectTargets(SkillCastContext context, List<Unit> results, Action<string> debugLog = null)
    {
        if (results == null)
            return false;

        results.Clear();

        if (context == null || context.Skill == null || context.Caster == null)
            return false;

        return context.Skill.Shape switch
        {
            SkillShape.SingleTarget => TryCollectSingleTarget(context, results, debugLog),
            SkillShape.Splash => TryCollectSplashTargets(context, results, debugLog),
            SkillShape.Area => TryCollectAreaTargets(context, results, debugLog),
            SkillShape.PiercingLine => TryCollectPiercingLineTargets(context, results, debugLog),
            SkillShape.Line => TryCollectLineTargets(context, results, debugLog),
            SkillShape.MultiTarget => TryCollectMultiTarget(context, results, debugLog),
            SkillShape.SpawnMinions => TryCollectSpawnMinionAnchor(context, results, debugLog),
            _ => false
        };
    }

    private static bool TryCollectSingleTarget(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        Unit primaryTarget = context != null ? context.PrimaryTarget : null;
        if (primaryTarget == null || !IsValidImpactTarget(context, primaryTarget))
            return false;

        results.Add(primaryTarget);
        debugLog?.Invoke(
            $"[SkillTargetCollector] {FormatUnit(context.Caster)} shape '{context.Skill.Shape}' resolved primary target " +
            $"{FormatUnit(primaryTarget)}. Impacted: {FormatUnits(results)}.");
        return true;
    }

    private static bool TryCollectSplashTargets(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        return TryCollectTargetsInRadius(context, results, debugLog, includePrimaryTargetFirst: true, "splash");
    }

    private static bool TryCollectAreaTargets(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        return TryCollectTargetsInRadius(context, results, debugLog, includePrimaryTargetFirst: false, "area");
    }

    private static bool TryCollectPiercingLineTargets(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        if (!TryBuildLineResolution(context, out LineResolution resolution))
            return false;

        for (int i = 0; i < resolution.Projections.Count; i++)
            TryAddUniqueTarget(results, resolution.Projections[i].Target);

        debugLog?.Invoke(
            $"[SkillTargetCollector] {FormatUnit(resolution.Caster)} shape '{resolution.Skill.Shape}' resolved piercing line. " +
            $"Primary: {FormatUnit(resolution.PrimaryTarget)}. Origin: {FormatWorldPosition(resolution.LineOrigin)}. " +
            $"Direction: {FormatWorldDirection(resolution.LineDirection)}. Length: {resolution.LineLengthInCells} cells ({resolution.LineLengthWorld:F2} world). " +
            $"Tolerance: {resolution.LineTolerance:F2}. Impacted ({results.Count}): {FormatUnits(results)}. " +
            $"Ordered projections: {FormatProjectedUnits(resolution.Projections)}. " +
            $"Skipped invalid/allied/dead: {resolution.SkippedInvalid}. Skipped behind caster: {resolution.SkippedBehindCaster}. " +
            $"Skipped past length: {resolution.SkippedPastLength}. Skipped off line: {resolution.SkippedOffLine}.");

        return results.Count > 0;
    }

    private static bool TryCollectLineTargets(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        if (!TryBuildLineResolution(context, out LineResolution resolution))
            return false;

        if (resolution.Projections.Count > 0)
            TryAddUniqueTarget(results, resolution.Projections[0].Target);

        debugLog?.Invoke(
            $"[SkillTargetCollector] {FormatUnit(resolution.Caster)} shape '{resolution.Skill.Shape}' resolved line. " +
            $"Primary: {FormatUnit(resolution.PrimaryTarget)}. Origin: {FormatWorldPosition(resolution.LineOrigin)}. " +
            $"Direction: {FormatWorldDirection(resolution.LineDirection)}. Length: {resolution.LineLengthInCells} cells ({resolution.LineLengthWorld:F2} world). " +
            $"Tolerance: {resolution.LineTolerance:F2}. First impact: {FormatUnit(results.Count > 0 ? results[0] : null)}. " +
            $"Ordered projections: {FormatProjectedUnits(resolution.Projections)}. " +
            $"Skipped invalid/allied/dead: {resolution.SkippedInvalid}. Skipped behind caster: {resolution.SkippedBehindCaster}. " +
            $"Skipped past length: {resolution.SkippedPastLength}. Skipped off line: {resolution.SkippedOffLine}.");

        return results.Count > 0;
    }

    private static bool TryCollectMultiTarget(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        Unit primaryTarget = context != null ? context.PrimaryTarget : null;
        if (primaryTarget == null || !IsValidImpactTarget(context, primaryTarget))
            return false;

        results.Add(primaryTarget);

        Unit caster = context.Caster;
        SkillData skill = context.Skill;
        RoomContext roomContext = caster != null ? caster.RoomContext : null;
        IReadOnlyList<Unit> roomUnits = roomContext != null ? roomContext.Units : null;
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
                if (ReferenceEquals(candidate, primaryTarget))
                {
                    skippedDuplicatePrimary++;
                    continue;
                }

                if (!IsValidImpactTarget(context, candidate))
                {
                    skippedInvalid++;
                    continue;
                }

                if (!IsWithinImpactRadius(context, primaryTarget, candidate))
                {
                    skippedOutOfRadius++;
                    continue;
                }

                candidates.Add(new DistanceCandidate(candidate, ResolveDistanceFromPrimary(context, primaryTarget, candidate)));
            }

            candidates.Sort(static (left, right) =>
            {
                int distanceComparison = left.Distance.CompareTo(right.Distance);
                if (distanceComparison != 0)
                    return distanceComparison;

                return left.Target.GetInstanceID().CompareTo(right.Target.GetInstanceID());
            });

            int remainingSlots = Mathf.Max(0, maxTargets - 1);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i < remainingSlots)
                {
                    TryAddUniqueTarget(results, candidates[i].Target);
                    continue;
                }

                skippedOverLimit++;
            }
        }

        debugLog?.Invoke(
            $"[SkillTargetCollector] {FormatUnit(caster)} shape '{skill.Shape}' resolved multi target. " +
            $"Primary: {FormatUnit(primaryTarget)}. Center: {FormatWorldPosition(primaryTarget.Position)}. " +
            $"Radius: {skill.ImpactRadiusInCells}. Max targets: {maxTargets}. " +
            $"Impacted ({results.Count}): {FormatUnits(results)}. " +
            $"Skipped duplicate primary: {skippedDuplicatePrimary}. " +
            $"Skipped invalid/allied/dead: {skippedInvalid}. Skipped out of radius: {skippedOutOfRadius}. " +
            $"Skipped over limit: {skippedOverLimit}.");

        return results.Count > 0;
    }

    private static bool TryCollectSpawnMinionAnchor(SkillCastContext context, List<Unit> results, Action<string> debugLog)
    {
        Unit primaryTarget = context != null ? context.PrimaryTarget : null;
        if (primaryTarget == null || !IsValidImpactTarget(context, primaryTarget))
            return false;

        results.Add(primaryTarget);
        debugLog?.Invoke(
            $"[SkillTargetCollector] {FormatUnit(context.Caster)} shape '{context.Skill.Shape}' resolved summon anchor. " +
            $"Primary: {FormatUnit(primaryTarget)}. Impacted: {FormatUnits(results)}.");
        return true;
    }

    private static bool TryCollectTargetsInRadius(
        SkillCastContext context,
        List<Unit> results,
        Action<string> debugLog,
        bool includePrimaryTargetFirst,
        string shapeName)
    {
        Unit primaryTarget = context != null ? context.PrimaryTarget : null;
        if (primaryTarget == null || !IsValidImpactTarget(context, primaryTarget))
            return false;

        if (includePrimaryTargetFirst)
            results.Add(primaryTarget);

        int skippedDuplicatePrimary = 0;
        int skippedInvalid = 0;
        int skippedOutOfRadius = 0;

        Unit caster = context.Caster;
        RoomContext roomContext = caster != null ? caster.RoomContext : null;
        IReadOnlyList<Unit> roomUnits = roomContext != null ? roomContext.Units : null;
        if (roomUnits == null)
        {
            if (!includePrimaryTargetFirst)
                results.Add(primaryTarget);

            debugLog?.Invoke(
                $"[SkillTargetCollector] {FormatUnit(context.Caster)} shape '{context.Skill.Shape}' resolved {shapeName} with only " +
                $"primary target {FormatUnit(primaryTarget)} because no room unit list was available.");
            return results.Count > 0;
        }

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (includePrimaryTargetFirst && ReferenceEquals(candidate, primaryTarget))
            {
                skippedDuplicatePrimary++;
                continue;
            }

            if (!IsValidImpactTarget(context, candidate))
            {
                skippedInvalid++;
                continue;
            }

            if (!IsWithinImpactRadius(context, primaryTarget, candidate))
            {
                skippedOutOfRadius++;
                continue;
            }

            TryAddUniqueTarget(results, candidate);
        }

        debugLog?.Invoke(
            $"[SkillTargetCollector] {FormatUnit(context.Caster)} shape '{context.Skill.Shape}' resolved {shapeName}. " +
            $"Primary: {FormatUnit(primaryTarget)}. Center: {FormatWorldPosition(primaryTarget.Position)}. " +
            $"Radius: {context.Skill.ImpactRadiusInCells}. " +
            $"Impacted ({results.Count}): {FormatUnits(results)}. " +
            $"Skipped duplicate primary: {skippedDuplicatePrimary}. " +
            $"Skipped invalid/allied/dead: {skippedInvalid}. Skipped out of radius: {skippedOutOfRadius}.");

        return results.Count > 0;
    }

    private static bool IsValidImpactTarget(SkillCastContext context, Unit candidate)
    {
        if (context == null || candidate == null)
            return false;

        Unit caster = context.Caster;
        if (caster == null)
            return false;

        if (!candidate.gameObject.activeInHierarchy || !candidate.IsAlive)
            return false;

        if (!ReferenceEquals(candidate.RoomContext, caster.RoomContext))
            return false;

        SkillRequirements requirements = context.Skill != null ? context.Skill.Requirements : null;
        return requirements == null || requirements.AreMet(caster, candidate);
    }

    private static bool IsWithinImpactRadius(SkillCastContext context, Unit primaryTarget, Unit candidate)
    {
        if (context == null || primaryTarget == null || candidate == null)
            return false;

        int radiusInCells = context.Skill != null ? context.Skill.ImpactRadiusInCells : 0;
        if (radiusInCells <= 0)
            return false;

        RoomGrid grid = context.Caster != null && context.Caster.RoomContext != null
            ? context.Caster.RoomContext.RoomGrid
            : null;

        if (grid == null)
        {
            float distance = Vector3.Distance(primaryTarget.Position, candidate.Position);
            return distance <= radiusInCells;
        }

        Vector3Int primaryCell = ResolveUnitCell(grid, primaryTarget);
        Vector3Int candidateCell = ResolveUnitCell(grid, candidate);
        return GridNavigationUtility.IsWithinCellRange(primaryCell, candidateCell, radiusInCells);
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        if (grid == null || unit == null)
            return Vector3Int.zero;

        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null && movement.TryGetLogicalCell(out Vector3Int cell))
            return cell;

        return grid.WorldToCell(unit.Position);
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

    private static float ResolveLineLengthWorld(SkillCastContext context, int lineLengthInCells)
    {
        if (context == null || context.Caster == null || context.Caster.RoomContext == null || context.Caster.RoomContext.RoomGrid == null)
            return Mathf.Max(0f, lineLengthInCells);

        Vector2 cellWorldSize = context.Caster.RoomContext.RoomGrid.CellWorldSize;
        float cellStep = Mathf.Max(Mathf.Abs(cellWorldSize.x), Mathf.Abs(cellWorldSize.y));
        return Mathf.Max(0f, lineLengthInCells) * Mathf.Max(0.01f, cellStep);
    }

    private static float ResolveLineTolerance(SkillCastContext context)
    {
        if (context == null || context.Caster == null || context.Caster.RoomContext == null || context.Caster.RoomContext.RoomGrid == null)
            return 0.5f;

        Vector2 cellWorldSize = context.Caster.RoomContext.RoomGrid.CellWorldSize;
        float cellStep = Mathf.Max(Mathf.Abs(cellWorldSize.x), Mathf.Abs(cellWorldSize.y));
        return Mathf.Max(0.1f, cellStep * 0.35f);
    }

    private static float ResolveDistanceFromPrimary(SkillCastContext context, Unit primaryTarget, Unit candidate)
    {
        if (context == null || primaryTarget == null || candidate == null)
            return float.MaxValue;

        RoomGrid grid = context.Caster != null && context.Caster.RoomContext != null
            ? context.Caster.RoomContext.RoomGrid
            : null;

        if (grid == null)
            return Vector3.Distance(primaryTarget.Position, candidate.Position);

        Vector3Int primaryCell = ResolveUnitCell(grid, primaryTarget);
        Vector3Int candidateCell = ResolveUnitCell(grid, candidate);
        return GridNavigationUtility.GetCellDistance(primaryCell, candidateCell);
    }

    private static bool TryBuildLineResolution(SkillCastContext context, out LineResolution resolution)
    {
        resolution = default;

        Unit caster = context != null ? context.Caster : null;
        Unit primaryTarget = context != null ? context.PrimaryTarget : null;
        SkillData skill = context != null ? context.Skill : null;
        if (caster == null || primaryTarget == null || skill == null || !IsValidImpactTarget(context, primaryTarget))
            return false;

        int lineLengthInCells = skill.LineLengthInCells;
        if (lineLengthInCells <= 0)
            return false;

        RoomContext roomContext = caster.RoomContext;
        IReadOnlyList<Unit> roomUnits = roomContext != null ? roomContext.Units : null;
        if (roomUnits == null)
            return false;

        Vector3 lineOrigin = caster.Position;
        Vector3 lineDirection = primaryTarget.Position - lineOrigin;
        lineDirection.z = 0f;

        if (lineDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        lineDirection.Normalize();

        float lineLengthWorld = ResolveLineLengthWorld(context, lineLengthInCells);
        float lineTolerance = ResolveLineTolerance(context);
        int skippedInvalid = 0;
        int skippedBehindCaster = 0;
        int skippedPastLength = 0;
        int skippedOffLine = 0;

        var projections = new List<TargetProjection>(roomUnits.Count);

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit candidate = roomUnits[i];
            if (!IsValidImpactTarget(context, candidate))
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

    private static void TryAddUniqueTarget(List<Unit> results, Unit candidate)
    {
        if (results == null || candidate == null || results.Contains(candidate))
            return;

        results.Add(candidate);
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

    private static string FormatUnit(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
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

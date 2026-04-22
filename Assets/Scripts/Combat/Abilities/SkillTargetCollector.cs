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
}

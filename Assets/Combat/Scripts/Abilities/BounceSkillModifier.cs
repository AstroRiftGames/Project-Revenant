using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BounceSkillModifier", menuName = "Combat/Skills/Modifiers/Bounce Skill Modifier")]
public class BounceSkillModifier : SkillModifier
{
    [SerializeField] private int _maxBounces = 1;
    [SerializeField] private int _bounceRangeInCells = 1;
    [SerializeField] private bool _canBounceToPrimaryTargetAgain;

    public override void ModifyImpacts(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        if (context == null || skill == null || impacts == null || impacts.Count == 0)
            return;

        int maxBounces = Mathf.Max(0, _maxBounces);
        int bounceRangeInCells = Mathf.Max(0, _bounceRangeInCells);
        if (maxBounces <= 0 || bounceRangeInCells <= 0)
            return;

        Unit caster = context.Caster;
        RoomGrid roomGrid = ResolveRoomGrid(context, caster);
        if (caster == null || roomGrid == null)
        {
            Warn(
                $"[BounceSkillModifier] Skill '{skill.DisplayName}' could not resolve a RoomGrid for bounce impacts.",
                skill);
            return;
        }

        IReadOnlyList<Unit> roomUnits = caster.GetRoomUnits();
        if (roomUnits == null || roomUnits.Count == 0)
            return;

        int sourceImpactCount = impacts.Count;
        for (int sourceImpactIndex = 0; sourceImpactIndex < sourceImpactCount; sourceImpactIndex++)
        {
            SkillImpact sourceImpact = impacts[sourceImpactIndex];
            if (sourceImpact == null || !sourceImpact.HasTargetUnit || sourceImpact.TargetUnit == null || !sourceImpact.TargetUnit.IsAlive)
                continue;

            Unit chainPrimaryUnit = sourceImpact.TargetUnit;
            Unit currentUnit = chainPrimaryUnit;
            int nextChainIndex = sourceImpact.ChainIndex + 1;
            bool primaryTargetRepeated = false;
            var visitedUnits = new List<Unit> { chainPrimaryUnit };

            for (int bounceIndex = 0; bounceIndex < maxBounces; bounceIndex++)
            {
                if (!TrySelectNextBounceTarget(
                        context,
                        roomGrid,
                        roomUnits,
                        impacts,
                        currentUnit,
                        chainPrimaryUnit,
                        bounceRangeInCells,
                        visitedUnits,
                        primaryTargetRepeated,
                        out Unit nextUnit,
                        out Vector3Int nextUnitCell))
                {
                    break;
                }

                if (ReferenceEquals(nextUnit, chainPrimaryUnit))
                    primaryTargetRepeated = true;

                Vector2Int nextCell2D = new(nextUnitCell.x, nextUnitCell.y);
                SkillImpact bounceImpact = SkillImpact.CreateUnit(nextUnit, nextCell2D, nextChainIndex, false);
                if (bounceImpact == null)
                    break;

                impacts.Add(bounceImpact);

                if (!ContainsVisitedUnit(visitedUnits, nextUnit))
                    visitedUnits.Add(nextUnit);

                currentUnit = nextUnit;
                nextChainIndex++;
            }
        }
    }

    private bool TrySelectNextBounceTarget(
        SkillContext context,
        RoomGrid roomGrid,
        IReadOnlyList<Unit> roomUnits,
        List<SkillImpact> impacts,
        Unit currentUnit,
        Unit chainPrimaryUnit,
        int bounceRangeInCells,
        List<Unit> visitedUnits,
        bool primaryTargetRepeated,
        out Unit nextUnit,
        out Vector3Int nextUnitCell)
    {
        nextUnit = null;
        nextUnitCell = default;

        if (context == null || roomGrid == null || roomUnits == null || currentUnit == null)
            return false;

        Vector3Int currentUnitCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, currentUnit);
        float bestDistance = float.MaxValue;
        int bestInstanceId = int.MaxValue;

        for (int unitIndex = 0; unitIndex < roomUnits.Count; unitIndex++)
        {
            Unit candidate = roomUnits[unitIndex];
            if (candidate == null || !candidate.IsAlive)
                continue;

            if (ReferenceEquals(candidate, currentUnit))
                continue;

            if (!SkillHitCollector.CanSkillHitUnit(context, candidate))
                continue;

            bool isPrimaryTargetCandidate = ReferenceEquals(candidate, chainPrimaryUnit);
            bool alreadyVisited = ContainsVisitedUnit(visitedUnits, candidate);
            bool canReusePrimaryTarget =
                isPrimaryTargetCandidate &&
                _canBounceToPrimaryTargetAgain &&
                !primaryTargetRepeated;

            if (alreadyVisited && !canReusePrimaryTarget)
                continue;

            if (ContainsUnitImpact(impacts, candidate) && !canReusePrimaryTarget)
                continue;

            Vector3Int candidateCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate);
            if (!GridNavigationUtility.IsWithinCellRange(currentUnitCell, candidateCell, bounceRangeInCells))
                continue;

            float distance = GridNavigationUtility.GetCellDistance(currentUnitCell, candidateCell);
            int candidateInstanceId = candidate.GetInstanceID();
            if (distance < bestDistance ||
                (Mathf.Approximately(distance, bestDistance) && candidateInstanceId < bestInstanceId))
            {
                bestDistance = distance;
                bestInstanceId = candidateInstanceId;
                nextUnit = candidate;
                nextUnitCell = candidateCell;
            }
        }

        return nextUnit != null;
    }

    private static RoomGrid ResolveRoomGrid(SkillContext context, Unit caster)
    {
        if (context != null && context.RoomGrid != null)
            return context.RoomGrid;

        return caster != null && caster.RoomContext != null
            ? caster.RoomContext.RoomGrid
            : null;
    }

    private static bool ContainsVisitedUnit(List<Unit> visitedUnits, Unit candidate)
    {
        if (visitedUnits == null || candidate == null)
            return false;

        for (int i = 0; i < visitedUnits.Count; i++)
        {
            if (ReferenceEquals(visitedUnits[i], candidate))
                return true;
        }

        return false;
    }

    private static bool ContainsUnitImpact(List<SkillImpact> impacts, Unit candidate)
    {
        if (impacts == null || candidate == null)
            return false;

        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            if (impact == null || !impact.HasTargetUnit)
                continue;

            if (ReferenceEquals(impact.TargetUnit, candidate))
                return true;
        }

        return false;
    }

    private static void Warn(string message, Object contextObject)
    {
        Debug.LogWarning(message, contextObject);
    }
}

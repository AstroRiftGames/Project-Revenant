using System.Collections.Generic;
using UnityEngine;

public static class RoomPlacementCellSelectionUtility
{
    public static void SortByEntryPriority(List<Vector3Int> candidateCells, RoomPlacementFrame placementFrame)
    {
        if (candidateCells == null)
            return;

        candidateCells.Sort((left, right) => CompareEntryPriority(left, right, placementFrame));
    }

    public static int CompareEntryPriority(Vector3Int left, Vector3Int right, RoomPlacementFrame placementFrame)
    {
        int leftForward = ResolveForwardDistance(left, placementFrame.AnchorCell, placementFrame.ForwardDirection);
        int rightForward = ResolveForwardDistance(right, placementFrame.AnchorCell, placementFrame.ForwardDirection);
        int forwardComparison = rightForward.CompareTo(leftForward);
        if (forwardComparison != 0)
            return forwardComparison;

        int leftLateral = ResolveLateralOffset(left, placementFrame.AnchorCell, placementFrame.LateralDirection);
        int rightLateral = ResolveLateralOffset(right, placementFrame.AnchorCell, placementFrame.LateralDirection);
        int lateralMagnitudeComparison = Mathf.Abs(leftLateral).CompareTo(Mathf.Abs(rightLateral));
        if (lateralMagnitudeComparison != 0)
            return lateralMagnitudeComparison;

        return rightLateral.CompareTo(leftLateral);
    }

    public static bool TryFindFormationCell(
        RoomGrid grid,
        Vector3Int desiredCell,
        RoomPlacementFrame placementFrame,
        HashSet<Vector3Int> reservedCells,
        int maxSearchRadius,
        out Vector3Int resultCell)
    {
        resultCell = desiredCell;
        int bestScore = int.MaxValue;
        bool found = false;

        for (int radius = 0; radius <= maxSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int candidateCell = desiredCell + new Vector3Int(x, y, 0);
                    if (!IsFormationCellCandidateValid(grid, candidateCell, reservedCells))
                        continue;

                    int forwardScore = ResolveFormationForwardScore(
                        candidateCell,
                        placementFrame.AnchorCell,
                        placementFrame.ForwardDirection);

                    if (forwardScore < 0)
                        continue;

                    int score = ResolveFormationCellScore(desiredCell, candidateCell, radius, forwardScore);
                    if (found && score >= bestScore)
                        continue;

                    bestScore = score;
                    resultCell = candidateCell;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    public static int ResolveForwardDistance(Vector3Int cell, Vector3Int anchorCell, Vector3Int forwardDirection)
    {
        return Mathf.RoundToInt(Vector3.Dot((Vector3)(cell - anchorCell), (Vector3)forwardDirection));
    }

    private static int ResolveLateralOffset(Vector3Int cell, Vector3Int anchorCell, Vector3Int lateralDirection)
    {
        return Mathf.RoundToInt(Vector3.Dot((Vector3)(cell - anchorCell), (Vector3)lateralDirection));
    }

    private static bool IsFormationCellCandidateValid(RoomGrid grid, Vector3Int candidateCell, HashSet<Vector3Int> reservedCells)
    {
        return grid != null &&
               grid.IsCellEnterable(candidateCell) &&
               (reservedCells == null || !reservedCells.Contains(candidateCell));
    }

    private static int ResolveFormationForwardScore(Vector3Int candidateCell, Vector3Int anchorCell, Vector3Int forwardDirection)
    {
        return Mathf.RoundToInt(Vector3.Dot((Vector3)(candidateCell - anchorCell), (Vector3)forwardDirection) * 100f);
    }

    private static int ResolveFormationCellScore(Vector3Int desiredCell, Vector3Int candidateCell, int radius, int forwardScore)
    {
        return (radius * 1000) +
               (GridNavigationUtility.GetCellDistance(desiredCell, candidateCell) * 10) -
               forwardScore;
    }
}

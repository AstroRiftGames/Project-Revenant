using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExplosiveSkillModifier", menuName = "Combat/Skills/Modifiers/Explosive Skill Modifier")]
public class ExplosiveSkillModifier : SkillModifier
{
    [SerializeField] private int _explosionRadiusInCells = 1;
    [SerializeField] private bool _includePrimaryImpactTarget = true;

    public int ExplosionRadiusInCells => Mathf.Max(0, _explosionRadiusInCells);
    public bool IncludePrimaryImpactTarget => _includePrimaryImpactTarget;

    public override void ModifyImpacts(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        if (context == null || skill == null || impacts == null || impacts.Count == 0)
            return;

        int explosionRadiusInCells = Mathf.Max(0, _explosionRadiusInCells);
        if (explosionRadiusInCells <= 0)
            return;

        Unit caster = context.Caster;
        RoomGrid roomGrid = ResolveRoomGrid(context, caster);
        if (caster == null)
        {
            Warn(
                $"[ExplosiveSkillModifier] Skill '{skill.DisplayName}' could not resolve a caster for explosive impacts.",
                skill);
            return;
        }

        IReadOnlyList<Unit> roomUnits = caster.GetRoomUnits();
        if (roomUnits == null || roomUnits.Count == 0)
            return;

        int sourceImpactCount = impacts.Count;
        for (int impactIndex = 0; impactIndex < sourceImpactCount; impactIndex++)
        {
            SkillImpact sourceImpact = impacts[impactIndex];
            if (sourceImpact == null)
                continue;

            if (!TryResolveExplosionCenter(roomGrid, sourceImpact, out Vector3Int explosionCenterCell, out Vector3 explosionCenterWorld))
            {
                Warn(
                    $"[ExplosiveSkillModifier] Skill '{skill.DisplayName}' ignored an impact because no explosion center could be resolved " +
                    $"for kind '{sourceImpact.Kind}'.",
                    skill);
                continue;
            }

            for (int unitIndex = 0; unitIndex < roomUnits.Count; unitIndex++)
            {
                Unit candidate = roomUnits[unitIndex];
                if (candidate == null || !candidate.IsAlive)
                    continue;

                if (!SkillHitCollector.CanSkillHitUnit(context, candidate))
                    continue;

                if (!IsCandidateInsideExplosion(roomGrid, explosionCenterCell, explosionCenterWorld, candidate, explosionRadiusInCells))
                    continue;

                bool isPrimaryTargetUnit =
                    sourceImpact.HasTargetUnit &&
                    ReferenceEquals(sourceImpact.TargetUnit, candidate);
                if (isPrimaryTargetUnit && !_includePrimaryImpactTarget)
                    continue;

                if (ContainsUnitImpact(impacts, candidate))
                    continue;

                SkillImpact explosiveImpact = CreateExplosiveImpact(roomGrid, candidate, sourceImpact.ChainIndex + 1);
                if (explosiveImpact == null)
                    continue;

                impacts.Add(explosiveImpact);
            }
        }
    }

    private static RoomGrid ResolveRoomGrid(SkillContext context, Unit caster)
    {
        if (context != null && context.RoomGrid != null)
            return context.RoomGrid;

        return caster != null && caster.RoomContext != null
            ? caster.RoomContext.RoomGrid
            : null;
    }

    private static bool TryResolveExplosionCenter(
        RoomGrid roomGrid,
        SkillImpact impact,
        out Vector3Int explosionCenterCell,
        out Vector3 explosionCenterWorld)
    {
        explosionCenterCell = default;
        explosionCenterWorld = default;
        if (impact == null)
            return false;

        if (impact.HasTargetUnit)
        {
            explosionCenterWorld = impact.TargetUnit.Position;
            if (roomGrid != null)
                explosionCenterCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, impact.TargetUnit);
            return true;
        }

        if (impact.Kind == SkillImpactKind.AreaPoint)
        {
            explosionCenterWorld = impact.WorldPosition;
            if (roomGrid != null)
                explosionCenterCell = roomGrid.WorldToCell(impact.WorldPosition);
            return true;
        }

        if (impact.HasCell && roomGrid != null)
        {
            explosionCenterCell = new Vector3Int(impact.Cell.x, impact.Cell.y, 0);
            explosionCenterWorld = roomGrid.CellToWorld(explosionCenterCell);
            return true;
        }

        return false;
    }

    private static bool IsCandidateInsideExplosion(
        RoomGrid roomGrid,
        Vector3Int explosionCenterCell,
        Vector3 explosionCenterWorld,
        Unit candidate,
        int explosionRadiusInCells)
    {
        if (candidate == null)
            return false;

        if (roomGrid != null)
        {
            Vector3Int candidateCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate);
            return GridNavigationUtility.IsWithinCellRange(explosionCenterCell, candidateCell, explosionRadiusInCells);
        }

        float distance = Vector3.Distance(explosionCenterWorld, candidate.Position);
        return distance <= Mathf.Max(0f, explosionRadiusInCells);
    }

    private static SkillImpact CreateExplosiveImpact(RoomGrid roomGrid, Unit candidate, int chainIndex)
    {
        if (candidate == null)
            return null;

        if (roomGrid != null)
        {
            Vector3Int candidateCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate);
            Vector2Int candidateCell2D = new(candidateCell.x, candidateCell.y);
            return SkillImpact.CreateUnit(candidate, candidateCell2D, chainIndex, false);
        }

        return SkillImpact.CreateUnit(candidate, chainIndex, false);
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

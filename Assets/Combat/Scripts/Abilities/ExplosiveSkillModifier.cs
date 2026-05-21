using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExplosiveSkillModifier", menuName = "Combat/Skills/Modifiers/Explosive Skill Modifier")]
public class ExplosiveSkillModifier : SkillModifier
{
    [SerializeField] private int _explosionRadiusInCells = 1;
    [SerializeField] private bool _includePrimaryImpactTarget = true;

    public override void ModifyImpacts(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
        if (context == null || skill == null || impacts == null || impacts.Count == 0)
            return;

        int explosionRadiusInCells = Mathf.Max(0, _explosionRadiusInCells);
        if (explosionRadiusInCells <= 0)
            return;

        Unit caster = context.Caster;
        RoomGrid roomGrid = ResolveRoomGrid(context, caster);
        if (caster == null || roomGrid == null)
        {
            Warn(
                $"[ExplosiveSkillModifier] Skill '{skill.DisplayName}' could not resolve a RoomGrid for explosive impacts.",
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

            if (!TryResolveExplosionCenterCell(roomGrid, sourceImpact, out Vector3Int explosionCenterCell))
            {
                Warn(
                    $"[ExplosiveSkillModifier] Skill '{skill.DisplayName}' ignored an impact because no explosion center cell could be resolved " +
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

                Vector3Int candidateCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, candidate);
                if (!GridNavigationUtility.IsWithinCellRange(explosionCenterCell, candidateCell, explosionRadiusInCells))
                    continue;

                bool isPrimaryTargetUnit =
                    sourceImpact.HasTargetUnit &&
                    ReferenceEquals(sourceImpact.TargetUnit, candidate);
                if (isPrimaryTargetUnit && !_includePrimaryImpactTarget)
                    continue;

                if (ContainsUnitImpact(impacts, candidate))
                    continue;

                Vector2Int candidateCell2D = new(candidateCell.x, candidateCell.y);
                SkillImpact explosiveImpact = SkillImpact.CreateUnit(
                    candidate,
                    candidateCell2D,
                    sourceImpact.ChainIndex + 1,
                    false);
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

    private static bool TryResolveExplosionCenterCell(RoomGrid roomGrid, SkillImpact impact, out Vector3Int explosionCenterCell)
    {
        explosionCenterCell = default;
        if (roomGrid == null || impact == null)
            return false;

        if (impact.HasCell)
        {
            explosionCenterCell = new Vector3Int(impact.Cell.x, impact.Cell.y, 0);
            return true;
        }

        if (impact.HasTargetUnit)
        {
            explosionCenterCell = GridUnitCellUtility.ResolveUnitCell(roomGrid, impact.TargetUnit);
            return true;
        }

        if (impact.Kind == SkillImpactKind.AreaPoint)
        {
            explosionCenterCell = roomGrid.WorldToCell(impact.WorldPosition);
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

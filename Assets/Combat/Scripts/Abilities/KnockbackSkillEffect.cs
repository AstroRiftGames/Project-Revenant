using UnityEngine;

[CreateAssetMenu(fileName = "KnockbackSkillEffect", menuName = "Combat/Skills/Effects/Knockback Skill Effect")]
public class KnockbackSkillEffect : SkillEffect
{
    [SerializeField] private int _knockbackCells = 1;
    [SerializeField] private bool _debugLogs;

    public int KnockbackCells => Mathf.Max(0, _knockbackCells);

    public override bool Apply(SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || !IsCombatAliveUnit(hitUnit))
            return false;

        return ApplyKnockback(context, hitUnit, caster);
    }

    private bool ApplyKnockback(SkillContext context, Unit target, Unit sourceUnit)
    {
        RoomGrid grid = ResolveRoomGrid(context, target, sourceUnit);
        UnitMovement movement = target.GetComponent<UnitMovement>();
        Unit referenceUnit = ResolveReferenceUnit(context, sourceUnit);

        Vector3 knockbackDirection = ResolveKnockbackDirection(target, referenceUnit);
        knockbackDirection.z = 0f;

        if (knockbackDirection.sqrMagnitude < Mathf.Epsilon)
        {
            LogDebug($"[KnockbackSkillEffect] {target.name} knockback aborted: zero direction.");
            return false;
        }

        int knockbackCells = Mathf.Max(0, _knockbackCells);

        if (grid != null)
        {
            Vector3Int currentCell = GridUnitCellUtility.ResolveUnitCell(grid, target);
            int stepX = Mathf.RoundToInt(knockbackDirection.x);
            int stepY = Mathf.RoundToInt(knockbackDirection.y);
            Vector3Int resolvedCell = currentCell;
            Vector3Int blockedCell = currentCell;
            bool hitBlocker = false;

            for (int i = 1; i <= knockbackCells; i++)
            {
                Vector3Int intermediateCell = currentCell + new Vector3Int(
                    stepX * i,
                    stepY * i,
                    0);

                if (!grid.IsCellEnterable(intermediateCell, target))
                {
                    blockedCell = intermediateCell;
                    hitBlocker = true;
                    break;
                }

                resolvedCell = intermediateCell;
            }

            if (movement != null)
            {
                if (!movement.ForceRelocateToCell(resolvedCell))
                {
                    LogDebug($"[KnockbackSkillEffect] {target.name} knockback relocation failed at cell {resolvedCell}.");
                    return false;
                }
            }
            else
            {
                target.transform.position = grid.CellToWorld(resolvedCell);
            }

            if (hitBlocker)
                LogDebug($"[KnockbackSkillEffect] {target.name} knocked back to cell {resolvedCell} (blocked at {blockedCell}).");
            else
                LogDebug($"[KnockbackSkillEffect] {target.name} knocked back to cell {resolvedCell}.");
            return true;
        }

        float worldDistance = knockbackCells;
        Vector3 newPosition = target.Position + knockbackDirection * worldDistance;
        if (movement != null)
            movement.ForceSyncToWorldPosition(newPosition);
        else
            target.transform.position = newPosition;

        LogDebug($"[KnockbackSkillEffect] {target.name} knocked back {worldDistance:F2} units (no grid).");
        return true;
    }

    private static RoomGrid ResolveRoomGrid(SkillContext context, Unit target, Unit sourceUnit)
    {
        if (context != null && context.RoomGrid != null)
            return context.RoomGrid;

        RoomGrid targetGrid = target != null ? target.RoomContext?.RoomGrid : null;
        if (targetGrid != null)
            return targetGrid;

        return sourceUnit != null ? sourceUnit.RoomContext?.RoomGrid : null;
    }

    private static Unit ResolveReferenceUnit(SkillContext context, Unit sourceUnit)
    {
        if (context == null)
            return sourceUnit;

        if (context.HasPrimaryTarget)
            return sourceUnit;

        if (context.HasImpactCenterUnit)
            return context.ImpactCenterUnit;

        return sourceUnit;
    }

    private static Vector3 ResolveKnockbackDirection(Unit target, Unit referenceUnit)
    {
        if (target == null || referenceUnit == null)
            return Vector3.zero;

        Vector3 direction = target.Position - referenceUnit.Position;
        direction.z = 0f;

        if (direction.sqrMagnitude < Mathf.Epsilon)
            return Vector3.zero;

        return direction.normalized;
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "KnockbackSkillEffect", menuName = "Combat/Skills/Effects/Knockback Skill Effect")]
public class KnockbackSkillEffect : SkillEffect
{
    [SerializeField] private int _knockbackCells = 1;
    [SerializeField] private bool _debugLogs;

    public override bool Apply(SkillCastContext context, Unit target)
    {
        if (context == null || context.Caster == null || target == null || !target.IsAlive)
            return false;

        Unit sourceUnit = context.Caster;
        if (sourceUnit == null)
        {
            LogDebug("[KnockbackSkillEffect] No source unit, aborting knockback.");
            return false;
        }

        ApplyKnockback(target, sourceUnit);
        return true;
    }

    private void ApplyKnockback(Unit target, Unit sourceUnit)
    {
        RoomGrid grid = target.RoomContext?.RoomGrid;
        UnitMovement movement = target.GetComponent<UnitMovement>();

        Vector3 knockbackDirection = (target.Position - sourceUnit.Position).normalized;
        knockbackDirection.z = 0f;

        if (knockbackDirection.sqrMagnitude < Mathf.Epsilon)
            knockbackDirection = Vector3.left;

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
                    return;
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
            return;
        }

        float worldDistance = knockbackCells;
        Vector3 newPosition = target.Position + knockbackDirection * worldDistance;
        if (movement != null)
            movement.ForceSyncToWorldPosition(newPosition);
        else
            target.transform.position = newPosition;

        LogDebug($"[KnockbackSkillEffect] {target.name} knocked back {worldDistance:F2} units (no grid).");
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}

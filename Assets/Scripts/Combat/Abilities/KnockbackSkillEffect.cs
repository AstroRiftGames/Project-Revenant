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

    private void SyncMovementWithPathfinding(Unit target)
    {
        UnitMovement movement = target.GetComponent<UnitMovement>();
        movement?.ForceSyncPosition();
    }

    private void ApplyKnockback(Unit target, Unit sourceUnit)
    {
        RoomGrid grid = target.RoomContext?.RoomGrid;

        Vector3 knockbackDirection = (target.Position - sourceUnit.Position).normalized;
        knockbackDirection.z = 0f;

        if (knockbackDirection.sqrMagnitude < Mathf.Epsilon)
            knockbackDirection = Vector3.left;

        int knockbackCells = Mathf.Max(0, _knockbackCells);

        if (grid != null)
        {
            Vector3Int currentCell = GridUnitCellUtility.ResolveUnitCell(grid, target);
            Vector3Int targetCell = currentCell + new Vector3Int(
                Mathf.RoundToInt(knockbackDirection.x) * knockbackCells,
                Mathf.RoundToInt(knockbackDirection.y) * knockbackCells,
                0);

            if (grid.IsCellEnterable(targetCell, target))
            {
                target.transform.position = grid.CellToWorld(targetCell);
                SyncMovementWithPathfinding(target);
                LogDebug($"[KnockbackSkillEffect] {target.name} knocked back to cell {targetCell}.");
                return;
            }

            for (int i = 1; i <= knockbackCells; i++)
            {
                Vector3Int intermediateCell = currentCell + new Vector3Int(
                    Mathf.RoundToInt(knockbackDirection.x) * i,
                    Mathf.RoundToInt(knockbackDirection.y) * i,
                    0);

                if (!grid.IsCellEnterable(intermediateCell, target))
                {
                    Vector3Int validCell = currentCell + new Vector3Int(
                        Mathf.RoundToInt(knockbackDirection.x) * (i - 1),
                        Mathf.RoundToInt(knockbackDirection.y) * (i - 1),
                        0);
                    target.transform.position = grid.CellToWorld(validCell);
                    SyncMovementWithPathfinding(target);
                    LogDebug($"[KnockbackSkillEffect] {target.name} knocked back to cell {validCell} (blocked at {intermediateCell}).");
                    return;
                }
            }
        }

        float worldDistance = knockbackCells;
        Vector3 newPosition = target.Position + knockbackDirection * worldDistance;
        target.transform.position = newPosition;
        SyncMovementWithPathfinding(target);
        LogDebug($"[KnockbackSkillEffect] {target.name} knocked back {worldDistance:F2} units (no grid).");
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
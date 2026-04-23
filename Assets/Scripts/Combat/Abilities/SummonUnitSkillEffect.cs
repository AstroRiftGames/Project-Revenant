using UnityEngine;

[CreateAssetMenu(fileName = "SummonUnitSkillEffect", menuName = "Combat/Skills/Effects/Summon Unit Skill Effect")]
public class SummonUnitSkillEffect : SkillEffect
{
    [SerializeField] private UnitData _summonedUnit;
    [SerializeField] private int _spawnRangeInCells = 1;
    [SerializeField] private bool _debugLogs;

    public override bool Apply(SkillCastContext context, Unit target)
    {
        if (context == null || context.Caster == null)
        {
            LogDebug("[SummonUnitSkillEffect] Aborted: missing cast context or caster.");
            return false;
        }

        if (_summonedUnit == null || _summonedUnit.unitPrefab == null)
        {
            LogDebug($"[SummonUnitSkillEffect] {FormatUnit(context.Caster)} aborted: no summoned unit prefab was assigned.");
            return false;
        }

        RoomContext roomContext = context.Caster.RoomContext;
        RoomGrid grid = roomContext != null ? roomContext.RoomGrid : null;
        if (roomContext == null || grid == null)
        {
            LogDebug($"[SummonUnitSkillEffect] {FormatUnit(context.Caster)} aborted: caster has no room context or room grid.");
            return false;
        }

        Vector3Int casterCell = ResolveUnitCell(grid, context.Caster);
        Vector3Int desiredCell = ResolveDesiredSpawnCell(grid, context.Caster, context.PrimaryTarget);
        int spawnRangeInCells = Mathf.Max(0, _spawnRangeInCells);

        if (!grid.TryFindWalkableCellInRange(desiredCell, casterCell, spawnRangeInCells, null, out Vector3Int spawnCell))
        {
            LogDebug(
                $"[SummonUnitSkillEffect] {FormatUnit(context.Caster)} aborted: no valid summon cell was found near {FormatCell(desiredCell)} " +
                $"within range {spawnRangeInCells}.");
            return false;
        }

        Vector3 spawnPosition = grid.CellToWorld(spawnCell);
        GameObject instance = Object.Instantiate(_summonedUnit.unitPrefab, spawnPosition, Quaternion.identity, roomContext.transform);
        if (!instance.TryGetComponent(out Unit summonedUnit))
        {
            Object.Destroy(instance);
            LogDebug($"[SummonUnitSkillEffect] {FormatUnit(context.Caster)} aborted: summoned prefab '{_summonedUnit.unitPrefab.name}' has no Unit component.");
            return false;
        }

        summonedUnit.SetAffiliation(context.Caster.Team, context.Caster.Faction);

        if (instance.TryGetComponent(out UnitMovement movement))
            movement.SetGrid(grid);
        else
            summonedUnit.SnapToGrid();

        LogDebug(
            $"[SummonUnitSkillEffect] {FormatUnit(context.Caster)} summoned '{_summonedUnit.displayName}' at {FormatCell(spawnCell)} " +
            $"from desired {FormatCell(desiredCell)} using skill '{context.Skill.DisplayName}'.");
        return true;
    }

    private static Vector3Int ResolveDesiredSpawnCell(RoomGrid grid, Unit caster, Unit primaryTarget)
    {
        Vector3Int casterCell = ResolveUnitCell(grid, caster);
        if (grid == null || caster == null || primaryTarget == null)
            return casterCell;

        Vector3Int targetCell = ResolveUnitCell(grid, primaryTarget);
        Vector3Int delta = targetCell - casterCell;
        int stepX = delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1);
        int stepY = delta.y == 0 ? 0 : (delta.y > 0 ? 1 : -1);
        return casterCell + new Vector3Int(stepX, stepY, 0);
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        if (grid == null || unit == null)
            return Vector3Int.zero;

        if (unit.TryGetComponent(out UnitMovement movement) && movement.TryGetLogicalCell(out Vector3Int cell))
            return cell;

        return grid.WorldToCell(unit.Position);
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message);
    }

    private static string FormatUnit(Unit unit)
    {
        if (unit == null)
            return "[None]";

        string unitId = !string.IsNullOrWhiteSpace(unit.Id) ? unit.Id : "NoUnitId";
        return $"[{unit.name}#{unit.GetInstanceID()}|{unitId}]";
    }

    private static string FormatCell(Vector3Int cell)
    {
        return $"({cell.x}, {cell.y}, {cell.z})";
    }
}

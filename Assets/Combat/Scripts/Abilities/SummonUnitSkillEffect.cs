using UnityEngine;

[CreateAssetMenu(fileName = "SummonUnitSkillEffect", menuName = "Combat/Skills/Effects/Summon Unit Skill Effect")]
public class SummonUnitSkillEffect : SkillEffect
{
    [SerializeField] private UnitData _summonedUnit;
    [SerializeField] private int _spawnRangeInCells = 1;
    [SerializeField] private bool _debugLogs;

    public override bool Apply(SkillContext context, Unit hitUnit)
    {
        Unit caster = context != null ? context.Caster : null;
        SkillData skill = context != null ? context.Skill : null;
        if (caster == null)
        {
            LogDebug("[SummonUnitSkillEffect] Aborted: missing caster.");
            return false;
        }

        if (_summonedUnit == null || _summonedUnit.unitPrefab == null)
        {
            LogDebug($"[SummonUnitSkillEffect] {FormatUnit(caster)} aborted: no summoned unit prefab was assigned.");
            return false;
        }

        RoomContext roomContext = ResolveRoomContext(context, caster);
        RoomGrid grid = ResolveRoomGrid(context, roomContext);
        if (roomContext == null || grid == null)
        {
            LogDebug($"[SummonUnitSkillEffect] {FormatUnit(caster)} aborted: caster has no room context or room grid.");
            return false;
        }

        Vector3Int casterCell = ResolveUnitCell(grid, caster);
        Vector3Int desiredCell = ResolveDesiredSpawnCell(context, grid, caster);
        int spawnRangeInCells = Mathf.Max(0, _spawnRangeInCells);

        if (!grid.TryFindWalkableCellInRange(desiredCell, casterCell, spawnRangeInCells, null, out Vector3Int spawnCell))
        {
            LogDebug(
                $"[SummonUnitSkillEffect] {FormatUnit(caster)} aborted: no valid summon cell was found near {FormatCell(desiredCell)} " +
                $"within range {spawnRangeInCells}.");
            return false;
        }

        Vector3 spawnPosition = grid.CellToWorld(spawnCell);
        GameObject instance = Object.Instantiate(_summonedUnit.unitPrefab, spawnPosition, Quaternion.identity, roomContext.transform);
        if (!instance.TryGetComponent(out Unit summonedUnit))
        {
            Object.Destroy(instance);
            LogDebug($"[SummonUnitSkillEffect] {FormatUnit(caster)} aborted: summoned prefab '{_summonedUnit.unitPrefab.name}' has no Unit component.");
            return false;
        }

        CombatSummonedUnitRuntimeMarker runtimeMarker = instance.GetComponent<CombatSummonedUnitRuntimeMarker>();
        if (runtimeMarker == null)
            runtimeMarker = instance.AddComponent<CombatSummonedUnitRuntimeMarker>();

        summonedUnit.SetAffiliation(caster.Team, caster.Faction);

        if (instance.TryGetComponent(out UnitMovement movement))
        {
            if (!movement.AttachToGridAtCell(grid, spawnCell))
            {
                Debug.LogWarning(
                    $"[SummonUnitSkillEffect] Primary attach failed for summoned unit '{summonedUnit.name}' at cell {FormatCell(spawnCell)}. " +
                    "Attempting safe fallback attach.",
                    instance);

                movement.SetGrid(grid);
                if (!movement.ForceSyncToCell(spawnCell))
                {
                    Debug.LogWarning(
                        $"[SummonUnitSkillEffect] Failed to safely attach summoned unit '{summonedUnit.name}' to cell {FormatCell(spawnCell)}.",
                        instance);
                    Object.Destroy(instance);
                    return false;
                }
            }
        }
        else
        {
            summonedUnit.SnapToGrid();
        }

        LogDebug(
            $"[SummonUnitSkillEffect] {FormatUnit(caster)} summoned '{_summonedUnit.displayName}' at {FormatCell(spawnCell)} " +
            $"from desired {FormatCell(desiredCell)} using skill '{skill?.DisplayName ?? "Unknown"}'.");
        return true;
    }

    private static RoomContext ResolveRoomContext(SkillContext context, Unit caster)
    {
        if (context != null && context.RoomContext != null)
            return context.RoomContext;

        return caster != null ? caster.RoomContext : null;
    }

    private static RoomGrid ResolveRoomGrid(SkillContext context, RoomContext roomContext)
    {
        if (context != null && context.RoomGrid != null)
            return context.RoomGrid;

        return roomContext != null ? roomContext.RoomGrid : null;
    }

    private static Vector3Int ResolveDesiredSpawnCell(SkillContext context, RoomGrid grid, Unit caster)
    {
        Vector3Int casterCell = ResolveUnitCell(grid, caster);
        if (context == null || grid == null)
            return casterCell;

        if (context.HasTargetCell)
            return new Vector3Int(context.TargetCell.x, context.TargetCell.y, 0);

        if (context.HasPrimaryTarget)
            return ResolveUnitCell(grid, context.PrimaryTarget);

        if (context.HasImpactCenterUnit)
            return ResolveUnitCell(grid, context.ImpactCenterUnit);

        return casterCell;
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        return GridUnitCellUtility.ResolveUnitCell(grid, unit);
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

public sealed class CombatSummonedUnitRuntimeMarker : MonoBehaviour
{
}

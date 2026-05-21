using UnityEngine;

public enum SkillImpactKind
{
    Unit,
    Cell,
    AreaPoint
}

public sealed class SkillImpact
{
    private SkillImpact(
        SkillImpactKind kind,
        Unit targetUnit,
        Vector2Int cell,
        bool hasCell,
        Vector3 worldPosition,
        int chainIndex,
        bool isPrimaryImpact)
    {
        Kind = kind;
        TargetUnit = targetUnit;
        Cell = cell;
        HasCell = hasCell;
        WorldPosition = worldPosition;
        ChainIndex = chainIndex;
        IsPrimaryImpact = isPrimaryImpact;
    }

    public Unit TargetUnit { get; }
    public bool HasTargetUnit => TargetUnit != null;
    public Vector2Int Cell { get; }
    public bool HasCell { get; }
    public Vector3 WorldPosition { get; }
    public SkillImpactKind Kind { get; }
    public int ChainIndex { get; set; }
    public bool IsPrimaryImpact { get; set; }

    public static SkillImpact CreateUnit(Unit targetUnit, int chainIndex, bool isPrimaryImpact)
    {
        if (targetUnit == null)
            return null;

        return new SkillImpact(
            SkillImpactKind.Unit,
            targetUnit,
            default,
            false,
            targetUnit.Position,
            chainIndex,
            isPrimaryImpact);
    }

    public static SkillImpact CreateCell(Vector2Int cell, int chainIndex, bool isPrimaryImpact)
    {
        return new SkillImpact(
            SkillImpactKind.Cell,
            null,
            cell,
            true,
            default,
            chainIndex,
            isPrimaryImpact);
    }

    public static SkillImpact CreateAreaPoint(Vector3 worldPosition, int chainIndex, bool isPrimaryImpact)
    {
        return new SkillImpact(
            SkillImpactKind.AreaPoint,
            null,
            default,
            false,
            worldPosition,
            chainIndex,
            isPrimaryImpact);
    }
}

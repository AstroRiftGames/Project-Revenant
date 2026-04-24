using System;
using UnityEngine;

public interface IInteractionAvailabilitySource
{
    bool IsInteractionAvailable { get; }
    event Action<bool> OnInteractionAvailabilityChanged;
}

public static class GridInteractionAvailability
{
    public static Necromancer ResolveNecromancer(Necromancer cachedNecromancer)
    {
        if (cachedNecromancer != null && cachedNecromancer.isActiveAndEnabled)
            return cachedNecromancer;

        return UnityEngine.Object.FindFirstObjectByType<Necromancer>();
    }

    public static bool IsNecromancerAdjacent(RoomGrid grid, Necromancer necromancer, Vector3 interactableWorldPosition)
    {
        if (grid == null || necromancer == null || !necromancer.isActiveAndEnabled)
            return false;

        if (!necromancer.TryGetGrid(out RoomGrid necromancerGrid) || !ReferenceEquals(necromancerGrid, grid))
            return false;

        Vector3Int interactableCell = grid.WorldToCell(interactableWorldPosition);
        Vector3Int necromancerCell = grid.WorldToCell(necromancer.transform.position);
        if (!grid.HasCell(necromancerCell))
            return false;

        return GridNavigationUtility.GetCellDistance(interactableCell, necromancerCell) <= 2;
    }
}

public static class StaticGridOccupancyUtility
{
    public static bool TryRegister(RoomGrid grid, IGridOccupant occupant, bool isOccupancyRegistered)
    {
        if (isOccupancyRegistered || grid == null || occupant == null)
            return isOccupancyRegistered;

        grid.OccupancyService.RegisterOccupant(occupant);
        return true;
    }

    public static bool Release(RoomGrid grid, IGridOccupant occupant, bool isOccupancyRegistered)
    {
        if (!isOccupancyRegistered || grid == null || occupant == null)
            return false;

        grid.OccupancyService.ReleaseOccupant(occupant);
        return false;
    }
}

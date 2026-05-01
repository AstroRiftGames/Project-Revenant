using UnityEngine;

public static class RoomGridResolver
{
    public static RoomGrid ResolveFromContext(RoomContext roomContext)
    {
        return roomContext != null ? roomContext.RoomGrid : null;
    }

    public static RoomGrid ResolveInParents(Component component, bool includeInactive = true)
    {
        if (component == null)
            return null;

        RoomContext roomContext = component.GetComponentInParent<RoomContext>(includeInactive);
        if (roomContext != null && roomContext.RoomGrid != null)
            return roomContext.RoomGrid;

        return component.GetComponentInParent<RoomGrid>(includeInactive);
    }
}

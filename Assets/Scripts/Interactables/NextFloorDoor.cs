using UnityEngine;
using PrefabDungeonGeneration;

public class NextFloorDoor : RoomDoor
{
    protected override void PerformInteraction()
    {
        PrepareNextFloorDestination();
        base.PerformInteraction();
    }

    private void PrepareNextFloorDestination()
    {
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        PrefabDungeonGenerator generator = FindFirstObjectByType<PrefabDungeonGenerator>();
        if (floorManager == null || generator == null)
            return;

        roomA = floorManager.CurrentRoom;

        if (roomB != null)
            return;

        floorManager.RequestNextFloor();
        roomB = generator.LastGeneratedStartRoom;

        LinkPreviousFloorDoor();
    }

    private void LinkPreviousFloorDoor()
    {
        if (roomB == null)
            return;

        PreviousFloorDoor previousFloorDoor = roomB.GetComponentInChildren<PreviousFloorDoor>(true);
        if (previousFloorDoor == null)
            return;

        previousFloorDoor.roomA = roomB;
        previousFloorDoor.roomB = roomA;
    }
}

using UnityEngine;
using PrefabDungeonGeneration;

public class NextFloorDoor : RoomDoor
{
    public override void Interact()
    {
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        PrefabDungeonGenerator generator = FindFirstObjectByType<PrefabDungeonGenerator>();
        
        if (floorManager != null && generator != null)
        {
            this.roomA = floorManager.CurrentRoom;

            floorManager.RequestNextFloor();

            this.roomB = generator.LastGeneratedStartRoom;
        }

        base.Interact();
    }
}

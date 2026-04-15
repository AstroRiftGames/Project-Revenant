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

            if (this.roomB == null)
            {
                floorManager.RequestNextFloor();

                this.roomB = generator.LastGeneratedStartRoom;

                if (this.roomB != null)
                {
                    PreviousFloorDoor prevDoor = this.roomB.GetComponentInChildren<PreviousFloorDoor>(true);
                    if (prevDoor != null)
                    {
                        prevDoor.roomA = this.roomB;
                        prevDoor.roomB = this.roomA;
                    }
                }
            }
        }

        base.Interact();
    }
}

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

            // Solo generamos un piso nuevo si no cruzamos por esta puerta antes
            if (this.roomB == null)
            {
                floorManager.RequestNextFloor();

                this.roomB = generator.LastGeneratedStartRoom;

                // Intentamos conectar automáticamente la puerta de vuelta si existe
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

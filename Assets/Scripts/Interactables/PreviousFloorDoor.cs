using UnityEngine;
using PrefabDungeonGeneration;

public class PreviousFloorDoor : RoomDoor
{
    private void Start()
    {
        PrefabDungeonGenerator generator = FindFirstObjectByType<PrefabDungeonGenerator>();
        if (generator != null && generator.FloorNumber <= 1)
        {
            gameObject.SetActive(false);
        }
    }
}

using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    private DungeonData dungeonData;
    [SerializeField] private RoomPrefabDatabase roomDatabase;
    private FloorData currentFloor;

    private FloorGenerator floorGenerator = new FloorGenerator();

    private void Start()
    {
        int seed = Random.Range(0, int.MaxValue);

        currentFloor = floorGenerator.Generate(1, seed);
        dungeonData.Floors.Add(currentFloor);
    }
    private Color GetColor(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return Color.green;
            case RoomType.Boss: return Color.red;
            case RoomType.MiniBoss: return new Color(1f, 0.5f, 0f);
            case RoomType.Loot: return Color.yellow;
            case RoomType.Shop: return Color.cyan;
            case RoomType.Combat: return Color.magenta;
            case RoomType.Altar: return Color.blue;
            default: return Color.gray;
        }
    }

    private void OnDrawGizmos()
    {
        if (currentFloor == null) return;

        foreach (var room in currentFloor.Rooms)
        {
            Vector3 pos = new Vector3(room.RoomID * 3, 0, 0);

            Gizmos.color = GetColor(room.RoomType);
            Gizmos.DrawSphere(pos, 0.5f);

            foreach (var connection in room.ConnectedRooms)
            {
                var target = currentFloor.Rooms.Find(r => r.RoomID == connection);
                if (target == null) continue;

                Vector3 targetPos = new Vector3(target.RoomID * 3, 0, 0);

                Gizmos.color = Color.white;
                Gizmos.DrawLine(pos, targetPos);
            }
        }
    }
}
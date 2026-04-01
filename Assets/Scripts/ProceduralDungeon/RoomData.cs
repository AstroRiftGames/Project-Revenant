using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProceduralDungeon
{
    public enum PRoomType
    {
        Start,
        Combat,
        Loot,
        Shop,
        Altar,
        MiniBoss,
        Boss
    }

    public enum DoorDirection
    {
        TopRight,
        BottomRight,
        BottomLeft,
        TopLeft
    }

    [System.Serializable]
    public class RoomSetting
    {
        public PRoomType RoomType;
        public Vector2Int MinSize = new Vector2Int(5, 5);
        public Vector2Int MaxSize = new Vector2Int(10, 10);
        public TileBase FloorTile;
        public TileBase WallTile;
    }

    [System.Serializable]
    public class DoorData
    {
        public DoorDirection Direction;
        public Vector2Int Position;
        public ProceduralRoom TargetRoom;

        public DoorData(DoorDirection direction, Vector2Int position, ProceduralRoom target)
        {
            Direction = direction;
            Position = position;
            TargetRoom = target;
        }
    }

    [System.Serializable]
    public class ProceduralRoom
    {
        public int ID;
        public RectInt Bounds;
        public PRoomType RoomType;
        public int Depth;

        public List<DoorData> Doors = new List<DoorData>();

        public void AddDoor(DoorDirection dir, Vector2Int pos, ProceduralRoom target)
        {
            Doors.Add(new DoorData(dir, pos, target));
        }

        public bool Intersects(RectInt other)
        {
            return Bounds.Overlaps(other);
        }
    }
}

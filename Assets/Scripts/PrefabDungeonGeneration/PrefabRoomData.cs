using System.Collections.Generic;
using UnityEngine;

namespace PrefabDungeonGeneration
{
    public enum PDRoomType
    {
        Start,
        Combat,
        Loot,
        Shop,
        Altar,
        MiniBoss,
        Boss
    }

    public enum PDDoorDirection
    {
        TopRight,
        BottomRight,
        BottomLeft,
        TopLeft
    }

    [System.Serializable]
    public class PDDoorAnchor
    {
        public Vector2Int Position;
        public PDDoorDirection Direction;
        
        public bool IsUsed;
    }

    public class PDRoomNode
    {
        public int ID;
        public PDRoomType RoomType;
        public Vector2Int WorldPosition;
        public Vector2Int Size;
        public RectInt Bounds => new RectInt(WorldPosition.x, WorldPosition.y, Size.x, Size.y);
        public RoomPrefabProfile PrefabProfile;
        public List<PDDoorAnchor> GlobalDoors = new List<PDDoorAnchor>();
        public int Depth;
    }

    public class PDFloorData
    {
        public int FloorIndex;
        public List<PDRoomNode> Rooms = new List<PDRoomNode>();
    }

    [System.Serializable]
    public class RoomTypeRule
    {
        public PDRoomType Type;
        public int Weight = 10;
        public int MinCount = 0;
        public int MaxCount = -1;
    }
}
